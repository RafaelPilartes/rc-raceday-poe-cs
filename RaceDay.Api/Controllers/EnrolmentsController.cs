using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Event entries. Participants enter events and see their own entries;
/// Organisers see who has entered their events.
/// </summary>
[ApiController]
[Produces("application/json")]
[SessionAuthorize]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Enters the logged-in Participant into an event under one category.</summary>
    /// <remarks>
    /// The participant comes from the session, so nobody can enter somebody
    /// else. The chosen category has to belong to the event in the route, and a
    /// participant may only hold one entry per event.
    /// </remarks>
    /// <param name="eventId">The event being entered.</param>
    /// <param name="request">The chosen category.</param>
    /// <response code="201">Entry created.</response>
    /// <response code="400">The category does not belong to this event.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is logged in as an Organiser.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="409">The Participant is already entered in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(MyEnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MyEnrolmentResponse>> Enrol(int eventId, CreateEnrolmentRequest request)
    {
        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {eventId}."));
        }

        var category = await db.Categories
            .SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId);

        if (category is null || category.EventId != eventId)
        {
            return BadRequest(new MessageResponse(
                $"Category {request.CategoryId} is not one of the categories for event {eventId}."));
        }

        var alreadyEntered = await db.Enrolments
            .AnyAsync(e => e.EventId == eventId && e.ParticipantId == CurrentUserId);

        if (alreadyEntered)
        {
            return Conflict(new MessageResponse("You are already entered in this event."));
        }

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatuses.Confirmed
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var response = new MyEnrolmentResponse(
            enrolment.EnrolmentId,
            raceEvent.EventId,
            raceEvent.EventName,
            raceEvent.EventDate,
            category.CategoryId,
            category.CategoryName,
            enrolment.EnrolmentDate,
            enrolment.Status);

        return CreatedAtAction(nameof(GetMyEnrolments), null, response);
    }

    /// <summary>Lists the logged-in Participant's own entries.</summary>
    /// <response code="200">The caller's entries, soonest event first.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is logged in as an Organiser.</response>
    [HttpGet("api/enrolments/my")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<MyEnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<MyEnrolmentResponse>>> GetMyEnrolments()
    {
        var enrolments = await db.Enrolments
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Event.EventDate)
            .Select(e => new MyEnrolmentResponse(
                e.EnrolmentId,
                e.EventId,
                e.Event.EventName,
                e.Event.EventDate,
                e.CategoryId,
                e.Category.CategoryName,
                e.EnrolmentDate,
                e.Status))
            .ToListAsync();

        return Ok(enrolments);
    }
}
