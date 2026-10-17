using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>Details a visitor supplies to open a RaceDay account.</summary>
public class RegisterRequest
{
    /// <summary>The user's full name, as it should appear on results lists.</summary>
    /// <example>Sipho Dlamini</example>
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email address, which doubles as the login name and must be unique.</summary>
    /// <example>sipho.dlamini@raceday.co.za</example>
    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password of at least eight characters. Only its BCrypt hash is stored.</summary>
    /// <example>Run10km!2026</example>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either Organiser or Participant.</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;
}

/// <summary>The account that was just created.</summary>
public record RegisteredUserResponse(int UserId, string FullName, string Email, string Role);

/// <summary>Credentials supplied at login.</summary>
public class LoginRequest
{
    /// <summary>The email address the account was registered with.</summary>
    /// <example>sipho.dlamini@raceday.co.za</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The account password.</summary>
    /// <example>Run10km!2026</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Who is now logged in, and under which role.</summary>
public record LoggedInUserResponse(int UserId, string FullName, string Role);
