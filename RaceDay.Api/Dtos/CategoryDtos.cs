using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>A category offered by one event.</summary>
public record CategoryResponse(
    int CategoryId,
    int EventId,
    string CategoryName,
    int? MinAge,
    int? MaxAge,
    decimal? DistanceKm);

/// <summary>The category fields an Organiser supplies when adding or editing a category.</summary>
public class SaveCategoryRequest
{
    /// <summary>Name shown to participants when they pick a category.</summary>
    /// <example>Open 20-39</example>
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Youngest age allowed, or null if the category is not age based.</summary>
    /// <example>20</example>
    [Range(0, 120)]
    public int? MinAge { get; set; }

    /// <summary>Oldest age allowed, or null if the category is not age based.</summary>
    /// <example>39</example>
    [Range(0, 120)]
    public int? MaxAge { get; set; }

    /// <summary>Distance for this category, or null to use the event distance.</summary>
    /// <example>10.00</example>
    [Range(0.01, 9999.99)]
    public decimal? DistanceKm { get; set; }
}
