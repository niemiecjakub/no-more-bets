using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match preview data from SoccerData API.</summary>
public record MatchPreviewDto
{
  public int Id { get; init; }
  public string Date { get; init; } = string.Empty;
  public string Time { get; init; } = string.Empty;
  public CountryInfo Country { get; init; } = null!;
  public LeagueInfo League { get; init; } = null!;
  public StageInfo Stage { get; init; } = null!;
  public Teams Teams { get; init; } = null!;
  public MatchData MatchData { get; init; } = null!;
  public IReadOnlyList<PreviewContentItem> PreviewContent { get; init; } = [];
}
public record LeagueInfo
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
}


public record StageInfo
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
  public bool IsActive { get; init; }
}

public record CountryInfo
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
}

