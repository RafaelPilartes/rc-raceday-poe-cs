using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>One row in the event listing.</summary>
public record EventSummaryResponse(
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerImageUrl,
    string OrganiserName);

/// <summary>One of the Organiser's own events, with how many people have entered it.</summary>
public record OrganiserEventResponse(
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerImageUrl,
    int EnrolmentCount);

/// <summary>Everything stored about a single event.</summary>
public record EventDetailResponse(
    int EventId,
    string EventName,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerImageUrl,
    int OrganiserId,
    string OrganiserName,
    DateTime CreatedAt);

/// <summary>The event fields an Organiser supplies when creating or updating an event.</summary>
public class SaveEventRequest
{
    /// <summary>Public name of the race.</summary>
    /// <example>Cape Town Coastal 10K</example>
    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Longer description shown on the event page.</summary>
    /// <example>A flat, scenic 10km run along the Sea Point promenade.</example>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Date and start time of the race.</summary>
    /// <example>2026-06-15T07:00:00</example>
    [Required]
    public DateTime EventDate { get; set; }

    /// <summary>Where the race starts.</summary>
    /// <example>Sea Point Promenade, Cape Town</example>
    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Main distance in kilometres.</summary>
    /// <example>10.00</example>
    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Link to a banner image for the event.</summary>
    /// <example>https://raceday.co.za/banners/coastal10k.jpg</example>
    [StringLength(255)]
    [Url]
    public string? BannerImageUrl { get; set; }
}
