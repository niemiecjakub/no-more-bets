using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// JSON cache manager with TTL support.
/// Handles saving and loading JSON responses from disk cache.
/// </summary>
public class JsonCache : BaseCache, IJsonCache
{
  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };

  public JsonCache(
      IOptions<JsonCacheOptions> options,
      ILogger<JsonCache> logger,
      IHostEnvironment env)
      : base(
          ResolveStoreFolder(options.Value.StoreFolder, env),
          options.Value.Store,
          options.Value.UseCache,
          options.Value.CacheTtlSeconds,
          logger)
  {
  }

  protected override string GetCacheKeyToFilename(string key, long? timestamp = null)
  {
    var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    return $"{key}_{ts}.json";
  }

  protected override long? ExtractTimestampFromFilename(string filename)
  {
    var match = Regex.Match(filename, @"_(\d+)(?:\.\w+)?$");
    if (match.Success && long.TryParse(match.Groups[1].Value, out var ts))
      return ts;
    return null;
  }

  protected override IReadOnlyList<string> FindCachedFiles(string key)
  {
    if (!Directory.Exists(StoreFolder))
      return [];

    var escaped = Regex.Escape(key);
    var pattern = new Regex($"^{escaped}_\\d+\\.json$", RegexOptions.Compiled);
    var list = new List<string>();

    foreach (var filePath in Directory.EnumerateFiles(StoreFolder, $"{key}_*.json"))
    {
      var fileName = Path.GetFileName(filePath);
      if (pattern.IsMatch(fileName))
        list.Add(filePath);
    }

    return list;
  }

  protected override async ValueTask<object?> ReadFileAsync(string filePath, CancellationToken cancellationToken)
  {
    await using var stream = File.OpenRead(filePath);
    var element = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    return element;
  }

  protected override async ValueTask WriteFileAsync(string filePath, object data, CancellationToken cancellationToken)
  {
    var bytes = JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions);
    await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);
  }

  public new async Task<JsonElement?> LoadAsync(string key, CancellationToken cancellationToken = default)
  {
    var result = await base.LoadAsync(key, cancellationToken).ConfigureAwait(false);
    return result as JsonElement?;
  }

  public Task SaveAsync(string key, JsonElement data, CancellationToken cancellationToken = default) =>
      base.SaveAsync(key, data, cancellationToken);

  public new Task<int> ClearAsync(string key, CancellationToken cancellationToken = default) =>
      base.ClearAsync(key, cancellationToken);

  private static string ResolveStoreFolder(string storeFolder, IHostEnvironment env)
  {
    if (Path.IsPathRooted(storeFolder))
      return storeFolder;
    return Path.Combine(env.ContentRootPath, storeFolder);
  }
}
