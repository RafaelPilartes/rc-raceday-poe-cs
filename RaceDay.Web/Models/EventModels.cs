namespace RaceDay.Web.Models;

public record OrganiserEvent(
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerImageUrl,
    int EnrolmentCount);

public record EventDetail(
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

public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };
}
