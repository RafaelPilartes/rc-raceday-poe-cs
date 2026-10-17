using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Race results. Organisers capture them once their event has been run;
/// Participants read back their own.
/// </summary>
[ApiController]
[Produces("application/json")]
[SessionAuthorize]
public class ResultsController : RaceDayControllerBase
{
    private const string TimeFormat = @"hh\:mm\:ss";

    private readonly RaceDayDbContext db;

    public ResultsController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Captures the finish time and position for one entry.</summary>
    /// <remarks>
    /// Only the Organiser who owns the event may capture its results, and only
    /// after the event date has passed. Each entry holds a single result, which
    /// is the one-to-one relationship in the Part 1 ERD.
    /// </remarks>
    /// <param name="enrolmentId">The entry the result belongs to.</param>
    /// <param name="request">Finish time as HH:MM:SS and the finishing position.</param>
    /// <response code="201">Result captured.</response>
    /// <response code="400">The time is badly formatted, or the event has not been run yet.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No entry with that id.</response>
    /// <response code="409">This entry already has a result.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CaptureResult(int enrolmentId, CaptureResultRequest request)
    {
        var enrolment = await db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null)
        {
            return NotFound(new MessageResponse($"No enrolment with id {enrolmentId}."));
        }

        if (enrolment.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only capture results for events you created.");
        }

        if (enrolment.Event.EventDate > DateTime.Now)
        {
            return BadRequest(new MessageResponse(
                $"{enrolment.Event.EventName} has not been run yet, so results cannot be captured."));
        }

        if (await db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
        {
            return Conflict(new MessageResponse("This enrolment already has a result."));
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = TimeSpan.Parse(request.FinishTime),
            FinishingPosition = request.FinishingPosition,
            CapturedAt = DateTime.Now
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        var response = new ResultResponse(
            result.ResultId,
            enrolment.EnrolmentId,
            enrolment.EventId,
            enrolment.Event.EventName,
            enrolment.ParticipantId,
            enrolment.Participant.FullName,
            enrolment.Category.CategoryName,
            result.FinishTime?.ToString(TimeFormat),
            result.FinishingPosition,
            result.CapturedAt);

        return CreatedAtAction(nameof(GetMyResults), null, response);
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <response code="200">The caller's results, most recent event first.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is logged in as an Organiser.</response>
    [HttpGet("api/results/my")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<MyResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<MyResultResponse>>> GetMyResults()
    {
        var rows = await db.Results
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(r => new
            {
                r.ResultId,
                r.Enrolment.EventId,
                r.Enrolment.Event.EventName,
                r.Enrolment.Event.EventDate,
                r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.FinishingPosition
            })
            .ToListAsync();

        // The time is formatted after the query because SQL Server has no
        // translation for TimeSpan.ToString(format).
        var results = rows
            .Select(row => new MyResultResponse(
                row.ResultId,
                row.EventId,
                row.EventName,
                row.EventDate,
                row.CategoryName,
                row.FinishTime?.ToString(TimeFormat),
                row.FinishingPosition))
            .ToList();

        return Ok(results);
    }
}
