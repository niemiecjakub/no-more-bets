namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Match preview data (excitement, prediction, weather, content).</summary>
public record MatchPreviewData
{
    public double ExcitementRating { get; init; }
    public required PredictionData Prediction { get; init; }
    public required WeatherData Weather { get; init; }
    public IReadOnlyList<PreviewContentItem> PreviewContent { get; init; } = [];
}
