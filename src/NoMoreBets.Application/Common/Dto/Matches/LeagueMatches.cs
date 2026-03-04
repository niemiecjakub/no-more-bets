namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Matches grouped by league.</summary>
public record LeagueMatches
{
    public int LeagueId { get; init; }
    public string LeagueName { get; init; } = string.Empty;
    public CountryInfo Country { get; init; } = null!;
    public bool IsCup { get; init; }
    public Season Season { get; init; } = null!;
    public IReadOnlyList<Stage> Stage { get; init; } = [];
}
