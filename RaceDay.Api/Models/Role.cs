namespace RaceDay.Api.Models;

/// <summary>
/// Lookup table for the two account types. Kept as its own entity, exactly as
/// in the Part 1 ERD, so a user's role is a foreign key and not free text.
/// </summary>
public class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
