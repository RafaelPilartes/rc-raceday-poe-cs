using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

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
    private readonly IBlobStorageService blobStorage;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobStorage)
    {
        this.db = db;
        this.blobStorage = blobStorage;
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

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// The organiser is taken from the session, so an Organiser cannot publish
    /// an event in somebody else's name.
    /// </remarks>
    /// <param name="request">The event details.</param>
    /// <response code="201">Event created. Returns the new event.</response>
    /// <response code="400">A field is missing or the event type is not Run, Walk or Cycle.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is logged in as a Participant.</response>
    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDetailResponse>> CreateEvent(SaveEventRequest request)
    {
        var matchedType = MatchEventType(request.EventType);
        if (matchedType is null)
        {
            return BadRequest(new MessageResponse(
                $"Event type must be one of {string.Join(", ", EventTypes.All)}."));
        }

        var raceEvent = new Event
        {
            OrganiserId = CurrentUserId,
            EventName = request.EventName.Trim(),
            Description = Blank(request.Description),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = matchedType,
            BannerImageUrl = Blank(request.BannerImageUrl)
        };

        db.Events.Add(raceEvent);
        await db.SaveChangesAsync();

        var organiserName = await db.Users
            .Where(u => u.UserId == raceEvent.OrganiserId)
            .Select(u => u.FullName)
            .SingleAsync();

        var response = Describe(raceEvent, organiserName);
        return CreatedAtAction(nameof(GetEvent), new { id = raceEvent.EventId }, response);
    }

    /// <summary>Updates an event the logged-in Organiser owns.</summary>
    /// <param name="id">The event id.</param>
    /// <param name="request">The replacement event details.</param>
    /// <response code="200">The event after the update.</response>
    /// <response code="400">A field is missing or the event type is not Run, Walk or Cycle.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> UpdateEvent(int id, SaveEventRequest request)
    {
        var matchedType = MatchEventType(request.EventType);
        if (matchedType is null)
        {
            return BadRequest(new MessageResponse(
                $"Event type must be one of {string.Join(", ", EventTypes.All)}."));
        }

        var raceEvent = await db.Events
            .Include(e => e.Organiser)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {id}."));
        }

        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change events you created.");
        }

        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = Blank(request.Description);
        raceEvent.EventDate = request.EventDate;
        raceEvent.Location = request.Location.Trim();
        raceEvent.DistanceKm = request.DistanceKm;
        raceEvent.EventType = matchedType;
        raceEvent.BannerImageUrl = Blank(request.BannerImageUrl);

        await db.SaveChangesAsync();

        return Ok(Describe(raceEvent, raceEvent.Organiser.FullName));
    }

    /// <summary>Deletes an event the logged-in Organiser owns.</summary>
    /// <remarks>
    /// The event's categories go with it. An event that already has
    /// participants enrolled is not deleted, because that would wipe their
    /// entries and any results captured against them.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    /// <response code="409">Participants are already enrolled in this event.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var raceEvent = await db.Events
            .Include(e => e.Categories)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {id}."));
        }

        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete events you created.");
        }

        if (await db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            return Conflict(new MessageResponse(
                "Participants are already enrolled in this event, so it cannot be deleted."));
        }

        db.Categories.RemoveRange(raceEvent.Categories);
        db.Events.Remove(raceEvent);
        await db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Uploads a banner image for an event the logged-in Organiser owns.</summary>
    /// <remarks>
    /// The file goes to Azure Blob Storage and the event keeps the public URL it
    /// came back with. Uploading again replaces the link; the previous image is
    /// left in the container.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <param name="bannerFile">A JPEG, PNG or WebP image of 5 MB or less.</param>
    /// <response code="200">The event with its new banner URL.</response>
    /// <response code="400">No file was sent, or it is too large or not an image.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("{id:int}/banner")]
    [SessionAuthorize(RoleNames.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> UploadBanner(int id, IFormFile? bannerFile)
    {
        var raceEvent = await db.Events
            .Include(e => e.Organiser)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {id}."));
        }

        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change the banner on events you created.");
        }

        var problem = ImageUpload.FindProblem(bannerFile);
        if (problem is not null)
        {
            return BadRequest(new MessageResponse(problem));
        }

        await using var content = bannerFile!.OpenReadStream();
        raceEvent.BannerImageUrl = await blobStorage.UploadImageAsync(
            content, bannerFile.FileName, bannerFile.ContentType);

        await db.SaveChangesAsync();

        return Ok(Describe(raceEvent, raceEvent.Organiser.FullName));
    }

    private static string? Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static EventDetailResponse Describe(Event raceEvent, string organiserName)
    {
        return new EventDetailResponse(
            raceEvent.EventId,
            raceEvent.EventName,
            raceEvent.Description,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.DistanceKm,
            raceEvent.EventType,
            raceEvent.BannerImageUrl,
            raceEvent.OrganiserId,
            organiserName,
            raceEvent.CreatedAt);
    }

    private static string? MatchEventType(string eventType)
    {
        return EventTypes.All.FirstOrDefault(
            type => string.Equals(type, eventType.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
