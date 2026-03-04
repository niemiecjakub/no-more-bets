namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match data including weather, excitement rating, and prediction.</summary>
public record MatchData
{
    public Weather Weather { get; init; } = null!;
    public double ExcitementRating { get; init; }
    public Prediction Prediction { get; init; } = null!;
}
