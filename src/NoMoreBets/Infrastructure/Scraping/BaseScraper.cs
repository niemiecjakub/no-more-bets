using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Storage;
using Polly;
using Polly.Retry;

namespace NoMoreBets.Infrastructure.Scraping;

/// <summary>
/// Base scraper with cache-first fetch, rate limiting, and retry with exponential backoff.
/// Concrete scrapers inherit and use <see cref="GetPageHtmlAsync"/> then parse to DTOs.
/// </summary>
public abstract class BaseScraper
{
    private readonly IHtmlCache _cache;
    private readonly IPageFetcher _fetcher;
    private readonly IInteractivePageFetcher _interactiveFetcher;
    private readonly BaseScraperOptions _options;
    private readonly ILogger _logger;
    private readonly ResiliencePipeline<string> _fetchPipeline;
    private readonly AsyncLocal<string?> _currentFetchUrl = new();
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private DateTimeOffset? _lastFetchTime;

    protected BaseScraper(
        IHtmlCache cache,
        IPageFetcher fetcher,
        IInteractivePageFetcher interactiveFetcher,
        IOptions<BaseScraperOptions> options,
        ILogger logger)
    {
        _cache = cache;
        _fetcher = fetcher;
        _interactiveFetcher = interactiveFetcher;
        _options = options.Value;
        _logger = logger;
        _fetchPipeline = CreateFetchPipeline(_options, _logger, _currentFetchUrl);
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
            _currentFetchUrl.Value = url;
            var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            try
            {
                return await _fetchPipeline.ExecuteAsync(async ct =>
                {
                    await RateLimitAsync(ct).ConfigureAwait(false);
                    var content = await _fetcher.GetHtmlAsync(url, timeout, ct).ConfigureAwait(false);
                    _lastFetchTime = DateTimeOffset.UtcNow;
                    await _cache.SaveAsync(url, content, ct).ConfigureAwait(false);
                    return content;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (PermanentScraperException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to fetch {url} after {_options.RetryCount} attempts", ex);
            }
        }
        finally
        {
            _currentFetchUrl.Value = null;
            _fetchLock.Release();
        }
    }

    /// <summary>
    /// Clears cached files for the given URL.
    /// </summary>
    /// <returns>Number of cache files removed.</returns>
    public Task<int> ClearCacheAsync(string url, CancellationToken cancellationToken = default) =>
        _cache.ClearAsync(url, cancellationToken);

    /// <summary>
    /// Gets page HTML after interactions: cache-first, then interactive fetch and save to cache.
    /// </summary>
    /// <param name="url">URL to fetch.</param>
    /// <param name="steps">Ordered list of interactions (e.g. click by selector).</param>
    /// <param name="timeout">Optional navigation timeout.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTML content after interactions.</returns>
    protected async Task<string> GetHtmlAfterInteractionsAsync(
        string url,
        IReadOnlyList<InteractionStep> steps,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cache.LoadAsync(url, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var html = await _interactiveFetcher.GetHtmlAfterInteractionsAsync(url, steps, timeout, cancellationToken).ConfigureAwait(false);
        await _cache.SaveAsync(url, html, cancellationToken).ConfigureAwait(false);
        return html;
    }

    /// <summary>
    /// Loads HTML from cache if available (e.g. before using interactive fetcher).
    /// </summary>
    protected Task<string?> LoadFromCacheAsync(string url, CancellationToken cancellationToken = default) =>
        _cache.LoadAsync(url, cancellationToken);

    /// <summary>
    /// Saves HTML to cache (e.g. after fetching via an interactive fetcher).
    /// </summary>
    protected Task SaveToCacheAsync(string url, string html, CancellationToken cancellationToken = default) =>
        _cache.SaveAsync(url, html, cancellationToken);

    private static ResiliencePipeline<string> CreateFetchPipeline(
        BaseScraperOptions options,
        ILogger logger,
        AsyncLocal<string?> currentFetchUrl)
    {
        var maxRetryAttempts = Math.Max(0, options.RetryCount - 1);
        var retryOptions = new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = maxRetryAttempts,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(options.RetryDelaySeconds),
            ShouldHandle = new PredicateBuilder<string>()
                .Handle<Exception>(ex => ex is not PermanentScraperException),
            OnRetry = args =>
            {
                var url = currentFetchUrl.Value ?? "unknown";
                logger.LogWarning(args.Outcome.Exception,
                    "Fetch attempt {Attempt}/{RetryCount} failed for {Url}",
                    args.AttemptNumber + 1, options.RetryCount, url);
                return ValueTask.CompletedTask;
            }
        };
        return new ResiliencePipelineBuilder<string>()
            .AddRetry(retryOptions)
            .Build();
    }

    private async Task RateLimitAsync(CancellationToken cancellationToken)
    {
        if (_lastFetchTime is null)
            return;

        var elapsed = DateTimeOffset.UtcNow - _lastFetchTime.Value;
        var delaySeconds = _options.DelaySeconds - elapsed.TotalSeconds;
        if (delaySeconds > 0)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
    }
}
