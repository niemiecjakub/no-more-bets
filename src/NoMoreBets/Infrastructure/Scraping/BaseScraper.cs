using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Infrastructure.Scraping;

/// <summary>
/// Base scraper with cache-first fetch, rate limiting, and retry with exponential backoff.
/// Concrete scrapers inherit and use <see cref="GetPageHtmlAsync"/> then parse to DTOs.
/// </summary>
public abstract class BaseScraper
{
    private readonly IHtmlCache _cache;
    private readonly IPageFetcher _fetcher;
    private readonly BaseScraperOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private DateTimeOffset? _lastFetchTime;

    protected BaseScraper(
        IHtmlCache cache,
        IPageFetcher fetcher,
        IOptions<BaseScraperOptions> options,
        ILogger logger)
    {
        _cache = cache;
        _fetcher = fetcher;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets page HTML: cache-first, then rate-limited fetch with retry and backoff.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML content of the page.</returns>
    /// <exception cref="PermanentScraperException">Permanent failure (403, 404, 410) – not retried.</exception>
    protected async Task<string> GetPageHtmlAsync(string url, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        if (cached is { } html)
            return html;

        await _fetchLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Exception? lastException = null;
            var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            for (var attempt = 1; attempt <= _options.RetryCount; attempt++)
            {
                await RateLimitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var content = await _fetcher.GetHtmlAsync(url, timeout, cancellationToken).ConfigureAwait(false);
                    _lastFetchTime = DateTimeOffset.UtcNow;
                    await _cache.SaveAsync(url, content, cancellationToken).ConfigureAwait(false);
                    return content;
                }
                catch (PermanentScraperException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Fetch attempt {Attempt}/{RetryCount} failed for {Url}", attempt, _options.RetryCount, url);
                }

                if (attempt < _options.RetryCount)
                {
                    var backoffSeconds = _options.RetryDelaySeconds * attempt * Jitter();
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), cancellationToken).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException(
                $"Failed to fetch {url} after {_options.RetryCount} attempts", lastException);
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    /// <summary>
    /// Clears cached files for the given URL.
    /// </summary>
    /// <returns>Number of cache files removed.</returns>
    public Task<int> ClearCacheAsync(string url, CancellationToken cancellationToken = default) =>
        _cache.ClearAsync(url, cancellationToken);

    private async Task RateLimitAsync(CancellationToken cancellationToken)
    {
        if (_lastFetchTime is null)
            return;

        var elapsed = DateTimeOffset.UtcNow - _lastFetchTime.Value;
        var delaySeconds = _options.DelaySeconds - elapsed.TotalSeconds;
        if (delaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
    }

    private static double Jitter() => 0.5 + (Random.Shared.NextDouble() * 1.0); // 0.5 .. 1.5
}
