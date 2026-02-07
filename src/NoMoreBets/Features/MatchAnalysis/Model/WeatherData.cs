namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Weather information for a match (MatchAnalysis-owned).</summary>
public record WeatherData
{
  public double TempC { get; init; }
  public string Description { get; init; } = string.Empty;
}
