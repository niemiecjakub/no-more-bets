namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Weather information for a match.</summary>
public record WeatherData
{
    public required string Description { get; init; }
    public double TempC { get; init; }
    public double TempF { get; init; }
}
