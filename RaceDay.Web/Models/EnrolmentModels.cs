namespace RaceDay.Web.Models;

public record EventEntry(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);
