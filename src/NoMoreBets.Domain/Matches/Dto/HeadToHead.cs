namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Head-to-head data between two teams from SoccerData API.</summary>
public record HeadToHead
{
  public TeamInfo Team1 { get; init; } = null!;
  public TeamInfo Team2 { get; init; } = null!;
  public HeadToHeadStats Stats { get; init; } = null!;
}

public record HeadToHeadStats
{
  public OverallStats Overall { get; init; } = null!;
  public Team1AtHomeStats Team1AtHome { get; init; } = null!;
  public Team2AtHomeStats Team2AtHome { get; init; } = null!;
}

public record OverallStats
{
  public int OverallGamesPlayed { get; init; }
  public int OverallTeam1Wins { get; init; }
  public int OverallTeam2Wins { get; init; }
  public int OverallDraws { get; init; }
  public int OverallTeam1Scored { get; init; }
  public int OverallTeam2Scored { get; init; }
}

public record Team1AtHomeStats
{
  public int Team1GamesPlayedAtHome { get; init; }
  public int Team1WinsAtHome { get; init; }
  public int Team1LossesAtHome { get; init; }
  public int Team1DrawsAtHome { get; init; }
  public int Team1ScoredAtHome { get; init; }
  public int Team1ConcededAtHome { get; init; }
}

public record Team2AtHomeStats
{
  public int Team2GamesPlayedAtHome { get; init; }
  public int Team2WinsAtHome { get; init; }
  public int Team2LossesAtHome { get; init; }
  public int Team2DrawsAtHome { get; init; }
  public int Team2ScoredAtHome { get; init; }
  public int Team2ConcededAtHome { get; init; }
}
