namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Single match from SoccerData API.</summary>
public record Match
{
    public int Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public Teams Teams { get; init; } = null!;
    public string Status { get; init; } = string.Empty;
    public int Minute { get; init; }
    public string Winner { get; init; } = string.Empty;
    public bool HasExtraTime { get; init; }
    public bool HasPenalties { get; init; }
    public Goals Goals { get; init; } = null!;
    public IReadOnlyList<MatchEvent> Events { get; init; } = [];
    public Odds Odds { get; init; } = null!;
    public MatchPreviewInfo MatchPreview { get; init; } = null!;
}
