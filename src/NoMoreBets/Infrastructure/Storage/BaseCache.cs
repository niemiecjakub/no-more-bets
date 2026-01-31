using Microsoft.Extensions.Logging;

namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// Base cache manager with TTL support.
/// Abstract base for cache implementations that save and load content from disk with time-to-live expiration.
/// </summary>
public abstract class BaseCache
{
    private readonly string _storeFolder;
    private readonly bool _store;
    private readonly bool _useCache;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger _logger;

    protected BaseCache(
        string storeFolder,
        bool store,
        bool useCache,
        double cacheTtlSeconds,
        ILogger logger)
    {
        _storeFolder = storeFolder;
        _store = store;
        _useCache = useCache;
        _cacheTtl = TimeSpan.FromSeconds(cacheTtlSeconds);
        _logger = logger;

        if (_store || _useCache)
            Directory.CreateDirectory(_storeFolder);
    }

    protected string StoreFolder => _storeFolder;

    protected abstract string GetCacheKeyToFilename(string key, long? timestamp = null);
    protected abstract long? ExtractTimestampFromFilename(string filename);
    protected abstract IReadOnlyList<string> FindCachedFiles(string key);
    protected abstract ValueTask<object?> ReadFileAsync(string filePath, CancellationToken cancellationToken);
    protected abstract ValueTask WriteFileAsync(string filePath, object data, CancellationToken cancellationToken);

    /// <summary>
    /// Loads cached content if available and not expired.
    /// </summary>
    /// <param name="key">Cache key to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached content if found and valid, null otherwise.</returns>
    public async Task<object?> LoadAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_useCache)
            return null;

        var cachedFiles = FindCachedFiles(key);
        if (cachedFiles.Count == 0)
            return null;

        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string? validFilepath = null;
        long? bestTimestamp = null;

        foreach (var filepath in cachedFiles)
        {
            var filename = Path.GetFileName(filepath);
            var timestamp = ExtractTimestampFromFilename(filename);
            if (timestamp is null)
                continue;

            var age = currentTime - timestamp.Value;
            if (age < _cacheTtl.TotalSeconds &&
                (bestTimestamp is null || timestamp > bestTimestamp))
            {
                validFilepath = filepath;
                bestTimestamp = timestamp;
            }
        }

        if (validFilepath is null)
            return null;

        try
        {
            var data = await ReadFileAsync(validFilepath, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Cache loaded for key: {Key}", key);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cache from {FilePath}", validFilepath);
            return null;
        }
    }

    /// <summary>
    /// Saves content to cache. Removes old cached files for the same key before saving.
    /// </summary>
    public async Task SaveAsync(string key, object data, CancellationToken cancellationToken = default)
    {
        if (!_store)
            return;

        var oldFiles = FindCachedFiles(key);
        foreach (var oldFile in oldFiles)
        {
            try
            {
                if (File.Exists(oldFile))
                    File.Delete(oldFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove old cache file {FilePath}", oldFile);
            }
        }

        var cacheFilename = GetCacheKeyToFilename(key);
        var cacheFilepath = Path.Combine(_storeFolder, cacheFilename);

        try
        {
            await WriteFileAsync(cacheFilepath, data, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save cache to {FilePath}", cacheFilepath);
        }
    }

    /// <summary>
    /// Clears all cached files for the given key.
    /// </summary>
    /// <returns>Number of files removed.</returns>
    public Task<int> ClearAsync(string key, CancellationToken cancellationToken = default)
    {
        var cachedFiles = FindCachedFiles(key);
        var removedCount = 0;

        foreach (var filepath in cachedFiles)
        {
            try
            {
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                    removedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache file {FilePath}", filepath);
            }
        }

        return Task.FromResult(removedCount);
    }
}
