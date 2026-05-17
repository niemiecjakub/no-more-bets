namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match data including weather, excitement rating, and prediction.</summary>
public record MatchData
{
  public Weather Weather { get; init; } = null!;
  public double ExcitementRating { get; init; }
  public Prediction Prediction { get; init; } = null!;
}

public record Weather
{
  public double TempF { get; init; }
  public double TempC { get; init; }
  public string Description { get; init; } = string.Empty;
}

public record Prediction
{
  public string Type { get; init; } = string.Empty;
  public string Choice { get; init; } = string.Empty;
}
