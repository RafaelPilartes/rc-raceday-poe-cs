using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>The finish time and position an Organiser captures for one entry.</summary>
public class CaptureResultRequest
{
    /// <summary>Finish time as HH:MM:SS.</summary>
    /// <example>00:52:18</example>
    [Required]
    [RegularExpression(
        @"^\d{1,2}:[0-5]\d:[0-5]\d$",
        ErrorMessage = "Finish time must be written as HH:MM:SS, for example 00:52:18.")]
    public string FinishTime { get; set; } = string.Empty;

    /// <summary>Where the participant placed overall.</summary>
    /// <example>47</example>
    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; set; }
}

/// <summary>A captured result, as the Organiser sees it.</summary>
public record ResultResponse(
    int ResultId,
    int EnrolmentId,
    int EventId,
    string EventName,
    int ParticipantId,
    string ParticipantName,
    string CategoryName,
    string? FinishTime,
    int? FinishingPosition,
    DateTime? CapturedAt);

/// <summary>One of the logged-in Participant's own results.</summary>
public record MyResultResponse(
    int ResultId,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    string? FinishTime,
    int? FinishingPosition);
