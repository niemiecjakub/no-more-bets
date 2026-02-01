namespace NoMoreBets.Features.Rotowire.Model;

/// <summary>
/// Represents a complete game with lineups.
/// </summary>
public record GameLineup
{
    public required string Date { get; init; }
    public string? Time { get; init; }
    public required TeamLineup HomeTeam { get; init; }
    public required TeamLineup AwayTeam { get; init; }
}
