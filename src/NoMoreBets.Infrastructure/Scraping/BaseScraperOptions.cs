namespace NoMoreBets.Infrastructure.Scraping;

/// <summary>
/// Options for base scraper: rate limiting, retry, and timeout.
/// Bind from config (e.g. "Scraper").
/// </summary>
public record BaseScraperOptions
{
    public const string SectionName = "Scraper";

    /// <summary>Minimum delay in seconds between fetches.</summary>
    public double DelaySeconds { get; init; } = 5.0;

    /// <summary>Number of retry attempts on transient failure.</summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>Base delay in seconds for exponential backoff between retries.</summary>
    public double RetryDelaySeconds { get; init; } = 2.0;

    /// <summary>Navigation timeout in seconds passed to the page fetcher.</summary>
    public double TimeoutSeconds { get; init; } = 15.0;

    /// <summary>Circuit breaker: failure ratio (0–1) to open the circuit.</summary>
    public double CircuitBreakerFailureRatio { get; init; } = 0.5;

    /// <summary>Circuit breaker: minimum number of failures before opening.</summary>
    public int CircuitBreakerMinimumThroughput { get; init; } = 5;

    /// <summary>Circuit breaker: duration in seconds the circuit stays open.</summary>
    public double CircuitBreakerBreakDurationSeconds { get; init; } = 30.0;
}
