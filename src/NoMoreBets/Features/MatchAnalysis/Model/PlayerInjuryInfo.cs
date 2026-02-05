namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Injury entry for a player (position, player name, status).</summary>
public record PlayerInjuryInfo
{
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required string Position { get; init; }
}
