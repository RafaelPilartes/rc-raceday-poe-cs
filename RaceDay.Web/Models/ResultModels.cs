namespace RaceDay.Web.Models;

public record MyResult(
    int ResultId,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    string? FinishTime,
    int? FinishingPosition,
    int TotalFinishers)
{
    /// Reads as "47th out of 312 finishers".
    public string Placing => FinishingPosition is null
        ? "Not placed"
        : $"{Ordinal(FinishingPosition.Value)} out of {TotalFinishers} finishers";

    private static string Ordinal(int position)
    {
        // 11th, 12th and 13th break the pattern the other numbers follow.
        if (position % 100 is >= 11 and <= 13)
        {
            return $"{position}th";
        }

        return (position % 10) switch
        {
            1 => $"{position}st",
            2 => $"{position}nd",
            3 => $"{position}rd",
            _ => $"{position}th"
        };
    }
}
