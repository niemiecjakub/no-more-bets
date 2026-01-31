namespace NoMoreBets.Infrastructure.ExternalClients;

/// <summary>
/// Fetches page HTML from a URL (e.g. via Playwright).
/// Used by BaseScraper to enable testing without a real browser.
/// </summary>
public interface IPageFetcher
{
    /// <summary>
    /// Navigates to the URL, waits for page load, and returns the HTML content.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="timeout">Optional navigation timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML content of the page.</returns>
    /// <exception cref="PermanentScraperException">Thrown for HTTP 403, 404, 410 (no retry).</exception>
    Task<string> GetHtmlAsync(
        string url,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
