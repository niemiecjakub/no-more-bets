using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NoMoreBets.Infrastructure.Scraping;

namespace NoMoreBets.Infrastructure.Fetching;

/// <summary>
/// Fetches page HTML using Playwright with WaitUntilState.NetworkIdle.
/// Throws <see cref="PermanentScraperException"/> for HTTP 403, 404, 410.
/// </summary>
public class PlaywrightPageFetcher : IPageFetcher
{
    private readonly ILogger<PlaywrightPageFetcher> _logger;

    public PlaywrightPageFetcher(ILogger<PlaywrightPageFetcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetHtmlAsync(
        string url,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var timeoutMs = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

        using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true }).ConfigureAwait(false);
        var page = await browser.NewPageAsync().ConfigureAwait(false);
        try
        {
            IResponse? response = null;
            try
            {
                response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = timeoutMs
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Playwright navigation failed for {Url}", url);
                throw;
            }

            if (response is not null)
            {
                var status = response.Status;
                if (status is 403 or 404 or 410)
                    throw new PermanentScraperException($"Permanent failure ({status}) for {url}", status);
            }

            var html = await page.ContentAsync().ConfigureAwait(false);
            return html ?? string.Empty;
        }
        finally
        {
            await page.CloseAsync().ConfigureAwait(false);
        }
    }
}
