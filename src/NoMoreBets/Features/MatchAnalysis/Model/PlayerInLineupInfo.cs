namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Name in a lineup (position and name).</summary>
public record PlayerInLineupInfo
{
    public required string Position { get; init; }
    public required string Name { get; init; }
}
