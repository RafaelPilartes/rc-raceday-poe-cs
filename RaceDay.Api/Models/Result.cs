namespace RaceDay.Api.Models;

/// <summary>
/// The finish time and position an Organiser captures for one enrolment after
/// the race. One result per enrolment, which is the one-to-one in the ERD.
/// </summary>
public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }

    public TimeSpan? FinishTime { get; set; }

    public int? FinishingPosition { get; set; }

    public DateTime? CapturedAt { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
}
