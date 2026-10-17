using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Age and distance categories. Listing sits under the event it belongs to;
/// editing and deleting address a category directly, as set out in the Part 1
/// plan.
/// </summary>
[ApiController]
[Produces("application/json")]
[SessionAuthorize]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext db;

    public CategoriesController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Lists the categories an event offers.</summary>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">The event's categories.</response>
    /// <response code="401">No active session.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetCategories(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound(new MessageResponse($"No event with id {eventId}."));
        }

        var categories = await db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryName)
            .Select(c => new CategoryResponse(
                c.CategoryId, c.EventId, c.CategoryName, c.MinAge, c.MaxAge, c.DistanceKm))
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a category to an event the logged-in Organiser owns.</summary>
    /// <param name="eventId">The event the category belongs to.</param>
    /// <param name="request">The category details.</param>
    /// <response code="201">Category created.</response>
    /// <response code="400">Name missing, or MinAge is greater than MaxAge.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(int eventId, SaveCategoryRequest request)
    {
        if (AgeRangeIsBackwards(request))
        {
            return BadRequest(new MessageResponse("MinAge cannot be greater than MaxAge."));
        }

        var raceEvent = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (raceEvent is null)
        {
            return NotFound(new MessageResponse($"No event with id {eventId}."));
        }

        if (raceEvent.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only add categories to events you created.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var response = Describe(category);
        return CreatedAtAction(nameof(GetCategories), new { eventId }, response);
    }

    /// <summary>Updates a category on one of the logged-in Organiser's events.</summary>
    /// <param name="id">The category id.</param>
    /// <param name="request">The replacement category details.</param>
    /// <response code="200">The category after the update.</response>
    /// <response code="400">Name missing, or MinAge is greater than MaxAge.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or the category belongs to another Organiser's event.</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, SaveCategoryRequest request)
    {
        if (AgeRangeIsBackwards(request))
        {
            return BadRequest(new MessageResponse("MinAge cannot be greater than MaxAge."));
        }

        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound(new MessageResponse($"No category with id {id}."));
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only change categories on events you created.");
        }

        category.CategoryName = request.CategoryName.Trim();
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;

        await db.SaveChangesAsync();

        return Ok(Describe(category));
    }

    /// <summary>Removes a category from one of the logged-in Organiser's events.</summary>
    /// <remarks>
    /// A category that participants have already entered is kept, so their
    /// enrolments keep pointing at something real.
    /// </remarks>
    /// <param name="id">The category id.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">The caller is a Participant, or the category belongs to another Organiser's event.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">Participants are already enrolled under this category.</response>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.Categories
            .Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound(new MessageResponse($"No category with id {id}."));
        }

        if (category.Event.OrganiserId != CurrentUserId)
        {
            return Forbidden("You can only delete categories on events you created.");
        }

        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            return Conflict(new MessageResponse(
                "Participants are already enrolled under this category, so it cannot be deleted."));
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static bool AgeRangeIsBackwards(SaveCategoryRequest request)
    {
        return request.MinAge is not null
            && request.MaxAge is not null
            && request.MinAge > request.MaxAge;
    }

    private static CategoryResponse Describe(Category category)
    {
        return new CategoryResponse(
            category.CategoryId,
            category.EventId,
            category.CategoryName,
            category.MinAge,
            category.MaxAge,
            category.DistanceKm);
    }
}
