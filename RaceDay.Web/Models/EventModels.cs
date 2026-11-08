using System.ComponentModel.DataAnnotations;

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

public record EventSummary(
    int EventId,
    string EventName,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerImageUrl,
    string OrganiserName);

public class EventSearchViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "On this date")]
    public DateTime? Date { get; set; }

    [Display(Name = "Location contains")]
    public string? Location { get; set; }

    [Display(Name = "Event type")]
    public string? EventType { get; set; }

    [Display(Name = "Hide races that have already been run")]
    public bool UpcomingOnly { get; set; } = true;

    public List<EventSummary> Events { get; set; } = new();

    public string ToQueryString()
    {
        var filters = new List<string>();

        if (Date is not null)
        {
            filters.Add($"date={Date.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(Location))
        {
            filters.Add($"location={Uri.EscapeDataString(Location.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(EventType))
        {
            filters.Add($"eventType={Uri.EscapeDataString(EventType)}");
        }

        if (UpcomingOnly)
        {
            filters.Add("upcomingOnly=true");
        }

        return filters.Count == 0 ? string.Empty : "?" + string.Join("&", filters);
    }
}

public class EventDetailsViewModel
{
    public EventDetail Event { get; set; } = null!;

    public List<EventCategory> Categories { get; set; } = new();
}
