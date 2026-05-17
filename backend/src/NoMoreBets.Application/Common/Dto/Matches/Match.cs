namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Single match from SoccerData API.</summary>
public record Match
{
  public int Id { get; init; }
  public string Date { get; init; } = string.Empty;
  public string Time { get; init; } = string.Empty;
  public Teams Teams { get; init; } = null!;
  public string Status { get; init; } = string.Empty;
  public int Minute { get; init; }
  public string Winner { get; init; } = string.Empty;
  public bool HasExtraTime { get; init; }
  public bool HasPenalties { get; init; }
  public Goals Goals { get; init; } = null!;
  public IReadOnlyList<MatchEvent> Events { get; init; } = [];
  public Odds Odds { get; init; } = null!;
  public MatchPreviewInfo MatchPreview { get; init; } = null!;
}

public record MatchPreviewInfo
{
  public bool HasPreview { get; init; }
  public int WordCount { get; init; }
}

public record MatchEvent
{
  public string EventType { get; init; } = string.Empty;
  public string EventMinute { get; init; } = string.Empty;
  public string Team { get; init; } = string.Empty;
  public Player? Player { get; init; }
  public Player? AssistPlayer { get; init; }
  public Player? PlayerIn { get; init; }
  public Player? PlayerOut { get; init; }
}

public record Goals
{
  public int HomeHtGoals { get; init; }
  public int AwayHtGoals { get; init; }
  public int HomeFtGoals { get; init; }
  public int AwayFtGoals { get; init; }
  public int HomeEtGoals { get; init; }
  public int AwayEtGoals { get; init; }
  public int HomePenGoals { get; init; }
  public int AwayPenGoals { get; init; }
}

public record Player
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
}
