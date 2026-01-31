namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents weather information for a game (condition, precipitation, temperature, wind).
/// </summary>
public record WeatherInfo
{
    public string? Condition { get; init; }
    public string? Precipitation { get; init; }
    public string? Temperature { get; init; }
    public string? Wind { get; init; }
}
