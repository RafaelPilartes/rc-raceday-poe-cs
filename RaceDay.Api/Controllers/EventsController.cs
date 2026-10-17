using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Events. Both roles may browse; only the Organiser who owns an event may
/// change or remove it.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
[SessionAuthorize]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext db;

    public EventsController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Lists events, soonest race date first, with optional filters.</summary>
    /// <remarks>
    /// All three filters are optional and combine with each other. Leave them
    /// off to get the full list.
    /// </remarks>
    /// <param name="date">Only events taking place on this calendar day.</param>
    /// <param name="location">Only events whose location contains this text.</param>
    /// <param name="eventType">Run, Walk or Cycle.</param>
    /// <response code="200">The matching events.</response>
    /// <response code="400">The event type filter was not Run, Walk or Cycle.</response>
    /// <response code="401">No active session.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EventSummaryResponse>>> GetEvents(
        [FromQuery] DateTime? date,
        [FromQuery] string? location,
        [FromQuery] string? eventType)
    {
        var query = db.Events.Include(e => e.Organiser).AsQueryable();

        if (date is not null)
        {
            var day = date.Value.Date;
            query = query.Where(e => e.EventDate.Date == day);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var text = location.Trim();
            query = query.Where(e => e.Location.Contains(text));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var matchedType = MatchEventType(eventType);
            if (matchedType is null)
            {
                return BadRequest(new MessageResponse(
                    $"Event type must be one of {string.Join(", ", EventTypes.All)}."));
            }

            query = query.Where(e => e.EventType == matchedType);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryResponse(
                e.EventId,
                e.EventName,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                e.EventType,
                e.BannerImageUrl,
                e.Organiser.FullName))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns the full detail of one event.</summary>
    /// <param name="id">The event id.</param>
    /// <response code="200">The event.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> GetEvent(int id)
    {
        var raceEvent = await db.Events
            .Include(e => e.Organiser)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {id}."));
        }

        return Ok(new EventDetailResponse(
            raceEvent.EventId,
            raceEvent.EventName,
            raceEvent.Description,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.DistanceKm,
            raceEvent.EventType,
            raceEvent.BannerImageUrl,
            raceEvent.OrganiserId,
            raceEvent.Organiser.FullName,
            raceEvent.CreatedAt));
    }

    private static string? MatchEventType(string eventType)
    {
        return EventTypes.All.FirstOrDefault(
            type => string.Equals(type, eventType.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
