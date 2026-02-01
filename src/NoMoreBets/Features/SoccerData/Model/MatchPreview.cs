namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Match preview data from SoccerData API.</summary>
public record MatchPreview
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
