namespace RaceDay.Api.Models;

/// <summary>
/// An age or distance category inside one event, defined by the event's Organiser.
/// MinAge, MaxAge and DistanceKm are all optional because a category can be
/// based on age, on distance, or on neither.
/// </summary>
public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int? MinAge { get; set; }

    public int? MaxAge { get; set; }

    public decimal? DistanceKm { get; set; }

    public Event Event { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
