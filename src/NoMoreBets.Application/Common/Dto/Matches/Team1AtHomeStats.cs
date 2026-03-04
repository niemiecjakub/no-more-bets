namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Statistics when team 1 plays at home.</summary>
public record Team1AtHomeStats
{
    public int Team1GamesPlayedAtHome { get; init; }
    public int Team1WinsAtHome { get; init; }
    public int Team1LossesAtHome { get; init; }
    public int Team1DrawsAtHome { get; init; }
    public int Team1ScoredAtHome { get; init; }
    public int Team1ConcededAtHome { get; init; }
}
