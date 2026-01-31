namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents betting odds for a game.
/// </summary>
public record GameOdds
{
    public string? HomeOdds { get; init; }
    public string? DrawOdds { get; init; }
    public string? AwayOdds { get; init; }
}
