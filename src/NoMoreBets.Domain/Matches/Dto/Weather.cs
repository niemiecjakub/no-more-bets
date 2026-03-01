namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Weather information for a match.</summary>
public record Weather
{
    public double TempF { get; init; }
    public double TempC { get; init; }
    public string Description { get; init; } = string.Empty;
}
