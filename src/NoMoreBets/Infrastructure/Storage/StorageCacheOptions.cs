namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// Options for disk cache with TTL support.
/// Bind from config (e.g. "StorageCache:JsonCache", "StorageCache:HtmlCache").
/// </summary>
public record StorageCacheOptions
{
    public string StoreFolder { get; init; } = "cache";
    public bool Store { get; init; } = true;
    public bool UseCache { get; init; } = true;
    public double CacheTtlSeconds { get; init; } = 3600.0;
}

/// <summary>
/// Options for JSON cache. Default folder "cache/json", TTL 24 hours.
/// </summary>
public record JsonCacheOptions
{
    public string StoreFolder { get; init; } = "cache/json";
    public bool Store { get; init; } = true;
    public bool UseCache { get; init; } = true;
    public double CacheTtlSeconds { get; init; } = 86400.0;
}

/// <summary>
/// Options for HTML cache. Default folder "cache/html", TTL from config (effectively indefinite when set high).
/// </summary>
public record HtmlCacheOptions
{
    public string StoreFolder { get; init; } = "cache/html";
    public bool Store { get; init; } = true;
    public bool UseCache { get; init; } = true;
    public double CacheTtlSeconds { get; init; } = 999_999_999_999_999_999.0;
}
