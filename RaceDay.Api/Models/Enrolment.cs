namespace RaceDay.Api.Models;

/// <summary>
/// Ties one Participant to one event under one category. A participant can
/// only hold a single enrolment per event, enforced by a unique index.
/// </summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }

    public int EventId { get; set; }

    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; }

    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public User Participant { get; set; } = null!;

    public Event Event { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}

public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Pending = "Pending";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Confirmed, Pending, Cancelled };
}
