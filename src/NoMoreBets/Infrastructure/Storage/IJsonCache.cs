using System.Text.Json;

namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// Cache for JSON responses with TTL support.
/// </summary>
public interface IJsonCache
{
    /// <summary>
    /// Loads cached JSON if available and not expired.
    /// </summary>
    Task<JsonElement?> LoadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves JSON to cache. Removes old entries for the same key first.
    /// </summary>
    Task SaveAsync(string key, JsonElement data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached files for the given key.
    /// </summary>
    Task<int> ClearAsync(string key, CancellationToken cancellationToken = default);
}
