using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Betclic.Scraping;

/// <summary>
/// Fetches and parses Betclic Premier League upcoming games and match bookmaker events.
/// </summary>
public interface IBetclicScraper
{
    /// <summary>
    /// Gets upcoming games from the Betclic Premier League page.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of upcoming games with teams, time, odds, and URL.</returns>
    Task<IReadOnlyList<UpcomingGame>> GetUpcomingGamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all bookmaker events (markets) for a specific match page.
    /// </summary>
    /// <param name="gameUrl">URL to the match page.</param>
    /// <param name="expand">If true, clicks consent/modal and "see more" before parsing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of bookmaker events with title and options.</returns>
    Task<IReadOnlyList<BookmakerEvent>> GetMatchEventsAsync(string gameUrl, bool expand, CancellationToken cancellationToken = default);
}
