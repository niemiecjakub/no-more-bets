namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// Cache for HTML content with TTL support.
/// </summary>
public interface IHtmlCache
{
    /// <summary>
    /// Loads cached HTML if available and not expired.
    /// </summary>
    Task<string?> LoadAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves HTML to cache. Removes old entries for the same URL first.
    /// </summary>
    Task SaveAsync(string url, string html, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached files for the given URL.
    /// </summary>
    Task<int> ClearAsync(string url, CancellationToken cancellationToken = default);
}
