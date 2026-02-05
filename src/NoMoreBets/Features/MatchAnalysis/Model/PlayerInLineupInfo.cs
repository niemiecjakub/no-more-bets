namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Name in a lineup (position and name).</summary>
public record PlayerInLineupInfo
{
    public required string Name { get; init; }
    public required string Position { get; init; }
}
