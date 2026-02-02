namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Single betting option (label and odds).</summary>
public record BettingOptionInfo
{
    public required string Label { get; init; }
    public double Odds { get; init; }
}
