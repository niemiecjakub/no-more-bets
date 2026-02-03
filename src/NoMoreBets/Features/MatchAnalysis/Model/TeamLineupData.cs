namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Team lineup data including players and injuries.</summary>
public record TeamLineupData
{
    public required string LineupTypeDisplayName { get; init; }
    public IReadOnlyList<PlayerInLineupInfo> Players { get; init; } = [];
    public IReadOnlyList<InjuryInfo> Injuries { get; init; } = [];
}
