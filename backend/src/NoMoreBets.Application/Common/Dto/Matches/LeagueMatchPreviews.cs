namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match previews grouped by league.</summary>
public record LeagueMatchPreviews
{
    public int LeagueId { get; init; }
    public string LeagueName { get; init; } = string.Empty;
    public IReadOnlyList<UpcomingMatchPreview> MatchPreviews { get; init; } = [];
}
