namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Lineup data for both teams.</summary>
public record LineupData
{
    public required TeamLineupData Home { get; init; }
    public required TeamLineupData Away { get; init; }
}
