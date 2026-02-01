namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Match prediction from SoccerData API.</summary>
public record Prediction
{
    public string Type { get; init; } = string.Empty;
    public string Choice { get; init; } = string.Empty;
}
