namespace NoMoreBets.Features.SoccerData;

/// <summary>
/// Options for SoccerData API client. Bind from config "SoccerData".
/// API key should be set via User Secrets or environment (e.g. SoccerData__ApiKey).
/// </summary>
public record SoccerDataOptions
{
    /// <summary>API key (auth_token). Required. Set via User Secrets or env.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Number of retry attempts on transient failure.</summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>Base delay in seconds between retries (exponential backoff).</summary>
    public double RetryDelaySeconds { get; init; } = 2.0;

    /// <summary>Request timeout in seconds.</summary>
    public double TimeoutSeconds { get; init; } = 15.0;
}
