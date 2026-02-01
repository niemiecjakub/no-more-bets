namespace NoMoreBets.Features.Fotmob.Scraping;

/// <summary>
/// Options for FotMob scraper (league). Bind from config "Scraper:Fotmob".
/// </summary>
public record FotmobScraperOptions
{
    /// <summary>League ID (e.g. 47 for Premier League).</summary>
    public int LeagueId { get; init; } = 47;

    /// <summary>League slug for URL path (e.g. premier-league).</summary>
    public string LeagueSlug { get; init; } = "premier-league";
}
