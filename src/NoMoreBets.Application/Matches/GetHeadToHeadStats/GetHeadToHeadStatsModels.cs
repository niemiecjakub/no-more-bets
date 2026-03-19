namespace NoMoreBets.Application.Matches.GetHeadToHeadStats;

public record H2H
{
  public string Summary { get; init; } = null!;
  public int TotalMatches { get; init; }
  public int TotalDraws { get; init; }
  public TeamMetrics TeamA { get; init; } = null!;
  public TeamMetrics TeamB { get; init; } = null!;
}

public record TeamMetrics
{
  public string Name { get; init; } = null!;
  public int TotalWins { get; init; }
  public int TotalGoalsScored { get; init; }
  public int TotalGoalsConceded { get; init; }
  public int HomeWins { get; init; }
  public int AwayWins { get; init; }
  public double WinPercentage { get; init; }
  public double AvgGoalsScored { get; init; }
  public double AvgGoalsConceded { get; init; }
}
