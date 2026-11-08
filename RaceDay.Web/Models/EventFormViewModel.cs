using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required]
    [StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Date and start time")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddMonths(1).AddHours(7);

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.01, 9999.99)]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = EventTypes.Run;

    /// Carried through the edit form as a hidden field so saving the other
    /// details does not clear a banner that was uploaded earlier.
    public string? BannerImageUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? BannerFile { get; set; }
}
