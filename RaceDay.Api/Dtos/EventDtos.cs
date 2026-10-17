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
