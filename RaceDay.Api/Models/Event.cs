namespace RaceDay.Api.Models;

/// <summary>
/// A race published by an Organiser. OrganiserId is the owner check every
/// write endpoint relies on.
/// </summary>
public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public decimal DistanceKm { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? BannerImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public User Organiser { get; set; } = null!;
}

public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };
}
