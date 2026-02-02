namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Basic match information (home, away, date, time).</summary>
public record MatchInfo
{
    public required string Home { get; init; }
    public required string Away { get; init; }
    public required string Date { get; init; }
    public required string Time { get; init; }
}
