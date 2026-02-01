using NoMoreBets.Infrastructure.Scraping;

namespace NoMoreBets.Infrastructure.Fetching;

/// <summary>
/// Fetches page HTML after performing a sequence of interactions (e.g. click consent, "see more").
/// Used for JS-heavy pages that require clicks before content is visible. Caller supplies selectors.
/// </summary>
public interface IInteractivePageFetcher
{
    /// <summary>
    /// Navigates to the URL, runs the interaction steps in order, then returns the HTML content.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="steps">Ordered list of interactions (e.g. click by selector).</param>
    /// <param name="timeout">Optional navigation timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML content after interactions.</returns>
    /// <exception cref="PermanentScraperException">Thrown for HTTP 403, 404, 410 (no retry).</exception>
    Task<string> GetHtmlAfterInteractionsAsync(
        string url,
        IReadOnlyList<InteractionStep> steps,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
