using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public record EventCategory(
    int CategoryId,
    int EventId,
    string CategoryName,
    int? MinAge,
    int? MaxAge,
    decimal? DistanceKm);

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120)]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.01, 9999.99)]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class EventCategoriesViewModel
{
    public EventDetail Event { get; set; } = null!;

    public List<EventCategory> Categories { get; set; } = new();

    public CategoryFormViewModel NewCategory { get; set; } = new();
}
