namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Bookmaker market/event with betting options.</summary>
public record BettingEventInfo
{
    public required string Title { get; init; }
    public IReadOnlyList<BettingOptionInfo> Options { get; init; } = [];
}
