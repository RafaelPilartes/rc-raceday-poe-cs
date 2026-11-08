using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public record EventEntry(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status,
    string? FinishTime,
    int? FinishingPosition);

public class CaptureResultViewModel
{
    public int EventId { get; set; }

    public int EnrolmentId { get; set; }

    /// Decides whether the result is created or corrected, because the API
    /// refuses a second capture on the same entry.
    public bool AlreadyCaptured { get; set; }

    [Required]
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$",
        ErrorMessage = "Write the finish time as HH:MM:SS, for example 00:52:18.")]
    [Display(Name = "Finish time")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    [Display(Name = "Position")]
    public int FinishingPosition { get; set; }
}
