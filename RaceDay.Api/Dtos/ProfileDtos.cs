using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>A user's own profile. The password hash is never part of this.</summary>
public record ProfileResponse(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? ProfilePictureUrl,
    DateTime CreatedAt);

/// <summary>The profile fields a user is allowed to change about themselves.</summary>
public class UpdateProfileRequest
{
    /// <summary>Replacement full name.</summary>
    /// <example>Sipho M. Dlamini</example>
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Link to a profile picture, or null to clear the current one.</summary>
    /// <example>https://raceday.co.za/avatars/sipho.jpg</example>
    [StringLength(255)]
    [Url]
    public string? ProfilePictureUrl { get; set; }
}
