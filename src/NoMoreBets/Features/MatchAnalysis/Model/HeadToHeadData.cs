namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Head-to-head statistics between two teams. Home and Away match the same roles as <see cref="MatchAnalysis.HomeTeam"/> and <see cref="MatchAnalysis.AwayTeam"/>.</summary>
public record HeadToHeadData
{
  public required TeamMatchup Home { get; init; }
  public required TeamMatchup Away { get; init; }
}

public record TeamMatchup
{
  public required string Name { get; init; }
  public required H2HStats H2HStats { get; init; }
}

public record H2HStats
{
  public required StatSummary Total { get; init; }
  public required StatSummary AtHome { get; init; }
}

public record StatSummary
{
  public required int Wins { get; init; }
  public required int Draws { get; init; }
  public required int Losses { get; init; }
  public required int GoalsScored { get; init; }

  // Calculated properties are excellent for LLM context
  public double WinRate => (Wins + Draws + Losses) > 0
      ? Math.Round((double)Wins / (Wins + Draws + Losses) * 100, 2)
      : 0;
}