using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile. Open to both roles.</summary>
[ApiController]
[Route("api/profile")]
[Produces("application/json")]
[SessionAuthorize]
public class ProfileController : RaceDayControllerBase
{
    private readonly RaceDayDbContext db;
    private readonly IBlobStorageService blobStorage;

    public ProfileController(RaceDayDbContext db, IBlobStorageService blobStorage)
    {
        this.db = db;
        this.blobStorage = blobStorage;
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

        return Ok(Describe(user));
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

        return Ok(Describe(user));
    }

    /// <summary>Uploads a profile picture for the logged-in user.</summary>
    /// <remarks>
    /// The picture always belongs to whoever is logged in, so one user cannot
    /// replace another user's photo. The file goes to Azure Blob Storage and the
    /// account keeps the public URL it came back with.
    /// </remarks>
    /// <param name="pictureFile">A JPEG, PNG or WebP image of 5 MB or less.</param>
    /// <response code="200">The profile with its new picture URL.</response>
    /// <response code="400">No file was sent, or it is too large or not an image.</response>
    /// <response code="401">No active session.</response>
    [HttpPost("picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> UploadProfilePicture(IFormFile? pictureFile)
    {
        var problem = ImageUpload.FindProblem(pictureFile);
        if (problem is not null)
        {
            return BadRequest(new MessageResponse(problem));
        }

        var user = await db.Users
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null)
        {
            return Unauthorized(new MessageResponse("The account on this session no longer exists."));
        }

        await using var content = pictureFile!.OpenReadStream();
        user.ProfilePictureUrl = await blobStorage.UploadImageAsync(
            content, pictureFile.FileName, pictureFile.ContentType);

        await db.SaveChangesAsync();

        return Ok(Describe(user));
    }

    private static ProfileResponse Describe(User user)
    {
        return new ProfileResponse(
            user.UserId,
            user.FullName,
            user.Email,
            user.Role.RoleName,
            user.ProfilePictureUrl,
            user.CreatedAt);
    }
}
