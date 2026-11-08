using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public record UserProfile(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? ProfilePictureUrl,
    DateTime CreatedAt);

public class ProfileFormViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// Hidden field, so saving the name does not clear a photo uploaded earlier.
    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "Profile picture")]
    public IFormFile? PictureFile { get; set; }
}
