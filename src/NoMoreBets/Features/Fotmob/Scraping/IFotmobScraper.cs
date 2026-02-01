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
}
