using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

/// <summary>The category a Participant picks when entering an event.</summary>
public class CreateEnrolmentRequest
{
    /// <summary>Id of a category that belongs to the event being entered.</summary>
    /// <example>2</example>
    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>One of the logged-in Participant's own entries.</summary>
public record MyEnrolmentResponse(
    int EnrolmentId,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);

/// <summary>One entry in an Organiser's start list for an event.</summary>
public record EventEnrolmentResponse(
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
