namespace NoMoreBets.Features.Betclic.Scraping;

/// <summary>
/// Options for Betclic scraper: empty-result retry behavior.
/// Bind from config (e.g. "Scraper:Betclic").
/// </summary>
public record BetclicScraperOptions
{
    public const string SectionName = "Scraper:Betclic";

    /// <summary>Number of retry attempts when parsing returns empty (clear cache and refetch). Default 5.</summary>
    public int EmptyResultRetryCount { get; init; } = 5;

    /// <summary>Minimum delay in seconds for jitter between empty-result retries. Default 3.</summary>
    public double EmptyResultRetryDelayMinSeconds { get; init; } = 3.0;

    /// <summary>Maximum delay in seconds for jitter between empty-result retries. Default 10.</summary>
    public double EmptyResultRetryDelayMaxSeconds { get; init; } = 10.0;

    /// <summary>Min delay for match-events empty retry (seconds). Default 8.</summary>
    public double MatchEventsRetryDelayMinSeconds { get; init; } = 8.0;

    /// <summary>Max delay for match-events empty retry (seconds). Default 20.</summary>
    public double MatchEventsRetryDelayMaxSeconds { get; init; } = 20.0;
}
