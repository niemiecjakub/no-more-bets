using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.RateLimiting;
using Polly.Timeout;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;

namespace NoMoreBets.Infrastructure.Scraping;

/// <summary>
/// Base scraper with Polly rate limiting, timeout, circuit breaker, and retry.
/// Concrete scrapers inherit and use <see cref="GetPageHtmlAsync"/> then parse to DTOs.
/// </summary>
public abstract class BaseScraper
{
  private readonly PlaywrightPageFetcher _pageFetcher;
  private readonly BaseScraperOptions _options;
  private readonly ILogger _logger;
  private readonly ResiliencePipeline<string> _fetchPipeline;
  private readonly ResiliencePipeline<string> _interactiveFetchPipeline;
  private readonly AsyncLocal<string?> _currentFetchUrl = new();

  protected BaseScraper(
      PlaywrightPageFetcher pageFetcher,
      IOptions<BaseScraperOptions> options,
      ILogger logger)
  {
    _pageFetcher = pageFetcher;
    _options = options.Value;
    _logger = logger;
    _fetchPipeline = CreateFetchPipeline(_options, _logger, _currentFetchUrl);
    _interactiveFetchPipeline = CreateInteractiveFetchPipeline(_options, _logger);
  }

  /// <summary>
  /// Gets page HTML: rate-limited fetch with timeout, circuit breaker, and retry.
  /// </summary>
  /// <param name="url">URL to fetch.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>HTML content of the page.</returns>
  /// <exception cref="PermanentScraperException">Permanent failure (403, 404, 410) – not retried.</exception>
  protected async Task<string> GetPageHtmlAsync(string url, CancellationToken cancellationToken = default)
  {
    _currentFetchUrl.Value = url;
    try
    {
      var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
      try
      {
        return await _fetchPipeline.ExecuteAsync(async ct =>
        {
          var content = await _pageFetcher.GetHtmlAsync(url, timeout, ct).ConfigureAwait(false);
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
    }
  }

  /// <summary>
  /// Gets page HTML after interactions via interactive fetch (with retry for transient failures).
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
      CancellationToken cancellationToken = default,
      string? waitForSelectorBeforeContent = null,
      string? waitForFunctionBeforeContent = null,
      bool blockStylesheets = true,
      bool blockResources = true)
  {
    var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_options.TimeoutSeconds);
    return await _interactiveFetchPipeline.ExecuteAsync(async ct =>
      await _pageFetcher.GetHtmlAfterInteractionsAsync(
          url,
          steps,
          effectiveTimeout,
          ct,
          waitForSelectorBeforeContent,
          waitForFunctionBeforeContent,
          blockStylesheets,
          blockResources).ConfigureAwait(false),
      cancellationToken).ConfigureAwait(false);
  }

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
          .Handle<Exception>(ex => ex is not PermanentScraperException)
          .Handle<RateLimiterRejectedException>(),
      DelayGenerator = args =>
      {
        if (args.Outcome.Exception is RateLimiterRejectedException { RetryAfter: { } retryAfter })
          return new ValueTask<TimeSpan?>(retryAfter);
        return new ValueTask<TimeSpan?>((TimeSpan?)null); // use default exponential backoff
      },
      OnRetry = args =>
      {
        var url = currentFetchUrl.Value ?? "unknown";
        logger.LogWarning(args.Outcome.Exception,
            "Fetch attempt {Attempt}/{RetryCount} failed for {Url}",
            args.AttemptNumber + 1, options.RetryCount, url);
        return ValueTask.CompletedTask;
      }
    };

    var circuitBreakerOptions = new CircuitBreakerStrategyOptions<string>
    {
      FailureRatio = options.CircuitBreakerFailureRatio,
      MinimumThroughput = options.CircuitBreakerMinimumThroughput,
      BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds),
      ShouldHandle = new PredicateBuilder<string>()
          .Handle<Exception>(ex => ex is not PermanentScraperException),
      OnOpened = args =>
      {
        logger.LogWarning("Scraper circuit breaker opened for {Duration}s",
            options.CircuitBreakerBreakDurationSeconds);
        return ValueTask.CompletedTask;
      },
      OnClosed = _ => ValueTask.CompletedTask,
      OnHalfOpened = _ => ValueTask.CompletedTask
    };

    var rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
    {
      // One permit per window per scraper instance; intentional to throttle fetches and reduce proxy/ban risk.
      // Concurrency is still 3 via Hangfire workers + context pool.
      PermitLimit = 1,
      Window = TimeSpan.FromSeconds(Math.Max(options.DelaySeconds, 0.01)),
      SegmentsPerWindow = 1
    });

    var timeoutOptions = new TimeoutStrategyOptions
    {
      Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds + 5), // pipeline timeout slightly above fetch timeout
      OnTimeout = args =>
      {
        logger.LogWarning("Fetch timed out for {Url}", currentFetchUrl.Value ?? "unknown");
        return ValueTask.CompletedTask;
      }
    };

    return new ResiliencePipelineBuilder<string>()
        .AddRetry(retryOptions)
        .AddCircuitBreaker(circuitBreakerOptions)
        .AddRateLimiter(new RateLimiterStrategyOptions
        {
          RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken)
        })
        .AddTimeout(timeoutOptions)
        .Build();
  }

  private static ResiliencePipeline<string> CreateInteractiveFetchPipeline(
      BaseScraperOptions options,
      ILogger logger)
  {
    var retryOptions = new RetryStrategyOptions<string>
    {
      MaxRetryAttempts = 2,
      BackoffType = DelayBackoffType.Exponential,
      UseJitter = true,
      Delay = TimeSpan.FromSeconds(1),
      ShouldHandle = new PredicateBuilder<string>()
          .Handle<Exception>(ex => ex is not PermanentScraperException),
      OnRetry = args =>
      {
        logger.LogWarning(args.Outcome.Exception,
            "Interactive fetch attempt {Attempt} failed", args.AttemptNumber + 1);
        return ValueTask.CompletedTask;
      }
    };

    return new ResiliencePipelineBuilder<string>()
        .AddRetry(retryOptions)
        .Build();
  }
}
