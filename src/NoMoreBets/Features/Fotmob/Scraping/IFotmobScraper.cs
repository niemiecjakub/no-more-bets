using NoMoreBets.Features.Fotmob.Model;

namespace NoMoreBets.Features.Fotmob.Scraping;

/// <summary>
/// FotMob scraper for league table and xG statistics.
/// </summary>
public interface IFotmobScraper
{
    /// <summary>
    /// Gets the league table (standings) for the configured league, optionally filtered by home/away/form.
    /// </summary>
    /// <param name="filter">Table filter (All, Home, Away, Form).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of clubs in table order.</returns>
    Task<IReadOnlyList<Club>> GetLeagueTableAsync(TableFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets xG statistics table for the configured league.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of xG stats per team.</returns>
    Task<IReadOnlyList<XgStats>> GetXgStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets club overview for a team (recent games and daily summary) from its overview page.
    /// </summary>
    /// <param name="teamId">Fotmob team ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Club overview with RecentGames (up to 5, oldest first) and DailySummary (text from list items).</returns>
    Task<ClubOverview> GetClubOverviewAsync(int teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets match details from a FotMob match detail page.
    /// </summary>
    /// <param name="gameUrl">FotMob match page URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Match details with home/away teams, match date, and lineups when present.</returns>
    Task<MatchDetails> GetMatchDetailsAsync(string gameUrl, CancellationToken cancellationToken = default);
}
