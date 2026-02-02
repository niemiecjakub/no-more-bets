namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Single recent game result (for future use).</summary>
public record RecentGameResult
{
    public required string OpponentName { get; init; }
    public required string Result { get; init; }
    public required string Date { get; init; }
}
