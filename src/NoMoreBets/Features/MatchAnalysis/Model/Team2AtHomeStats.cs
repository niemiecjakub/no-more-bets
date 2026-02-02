namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Statistics when team 2 plays at home.</summary>
public record Team2AtHomeStats
{
    public int Team2GamesPlayedAtHome { get; init; }
    public int Team2WinsAtHome { get; init; }
    public int Team2LossesAtHome { get; init; }
    public int Team2DrawsAtHome { get; init; }
    public int Team2ScoredAtHome { get; init; }
    public int Team2ConcededAtHome { get; init; }
}
