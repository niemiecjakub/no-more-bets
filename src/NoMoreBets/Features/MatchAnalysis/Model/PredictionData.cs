namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Match prediction with derived team name.</summary>
public record PredictionData
{
    public required string Type { get; init; }
    public required string Choice { get; init; }
    public required string TeamName { get; init; }
}
