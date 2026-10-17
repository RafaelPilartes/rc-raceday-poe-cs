namespace RaceDay.Api.Models;

/// <summary>
/// Both Organisers and Participants live here; RoleId decides which one a row is.
/// </summary>
public class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
