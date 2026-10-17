using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile. Open to both roles.</summary>
[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[SessionAuthorize]
public class ProfileController : RaceDayControllerBase
{
    private readonly RaceDayDbContext db;

    public ProfileController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Returns the profile of whoever is logged in.</summary>
    /// <remarks>
    /// The user id comes from the session, never from the request, so a user
    /// cannot ask for somebody else's profile through this endpoint.
    /// </remarks>
    /// <response code="200">The caller's own profile.</response>
    /// <response code="401">No active session.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> GetMyProfile()
    {
        var user = await db.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null)
        {
            return Unauthorized(new MessageResponse("The account on this session no longer exists."));
        }

        return Ok(new ProfileResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.ProfilePictureUrl,
            user.CreatedAt));
    }

    /// <summary>Updates the caller's own name and profile picture.</summary>
    /// <remarks>
    /// Email and role are deliberately left out: changing either would affect
    /// how the account logs in and what it is allowed to do, so neither is
    /// editable from the profile screen.
    /// </remarks>
    /// <param name="request">The new full name and optional profile picture URL.</param>
    /// <response code="200">The profile after the update.</response>
    /// <response code="400">Full name missing, or the picture URL is not a valid link.</response>
    /// <response code="401">No active session.</response>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> UpdateMyProfile(UpdateProfileRequest request)
    {
        var user = await db.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null)
        {
            return Unauthorized(new MessageResponse("The account on this session no longer exists."));
        }

        user.FullName = request.FullName.Trim();
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
            ? null
            : request.ProfilePictureUrl.Trim();

        await db.SaveChangesAsync();

        return Ok(new ProfileResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.ProfilePictureUrl,
            user.CreatedAt));
    }
}
