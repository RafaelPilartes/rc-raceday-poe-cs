using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

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
}
