using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext db;

    public AuthController(RaceDayDbContext db)
    {
        this.db = db;
    }

    /// <summary>Creates a new account as either an Organiser or a Participant.</summary>
    /// <remarks>
    /// The password is hashed with BCrypt before it is saved; the plain text
    /// never reaches the database. Email addresses are stored lower case and
    /// have to be unique across both roles.
    /// </remarks>
    /// <param name="request">Full name, email, password and the chosen role.</param>
    /// <response code="201">Account created. Returns the new user id and role.</response>
    /// <response code="400">A field is missing, the password is too short, or the role is not one of the two valid values.</response>
    /// <response code="409">That email address already has an account.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisteredUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisteredUserResponse>> Register(RegisterRequest request)
    {
        var chosenRole = RoleNames.All.FirstOrDefault(
            name => string.Equals(name, request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (chosenRole is null)
        {
            return BadRequest(new MessageResponse(
                $"Role must be either {RoleNames.Organiser} or {RoleNames.Participant}."));
        }

        var role = await db.Roles.SingleOrDefaultAsync(r => r.RoleName == chosenRole);
        if (role is null)
        {
            return BadRequest(new MessageResponse(
                $"The {chosenRole} role is missing from the database. Run the migrations first."));
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new MessageResponse("That email address is already registered."));
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var response = new RegisteredUserResponse(user.UserId, user.FullName, user.Email, role.RoleName);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Checks the credentials and starts a session for the user.</summary>
    /// <remarks>
    /// On success the user id and role are written to the server-side session
    /// and the caller gets back the RaceDay.Session cookie. Every protected
    /// endpoint reads the role from that session, so the cookie has to be sent
    /// with the requests that follow. Swagger does this automatically.
    /// </remarks>
    /// <param name="request">Email address and password.</param>
    /// <response code="200">Logged in. Returns the user id, name and role.</response>
    /// <response code="400">Email or password was left out.</response>
    /// <response code="401">No account with that email, or the password does not match.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoggedInUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoggedInUserResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);

        // Same message either way, so the response cannot be used to work out
        // which email addresses exist.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new MessageResponse("Email address or password is incorrect."));
        }

        HttpContext.Session.Clear();
        HttpContext.Session.StartUserSession(user.UserId, user.Role.RoleName);

        return Ok(new LoggedInUserResponse(user.UserId, user.FullName, user.Role.RoleName));
    }
}
