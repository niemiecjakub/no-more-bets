namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Match previews grouped by league.</summary>
public record LeagueMatchPreviews
{
    public int LeagueId { get; init; }
    public string LeagueName { get; init; } = string.Empty;
    public IReadOnlyList<UpcomingMatchPreview> MatchPreviews { get; init; } = [];
}
