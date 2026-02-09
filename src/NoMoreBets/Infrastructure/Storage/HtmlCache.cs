using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NoMoreBets.Infrastructure.Storage;

/// <summary>
/// HTML cache manager with TTL support.
/// Handles saving and loading HTML content from disk cache.
/// </summary>
public class HtmlCache : BaseCache, IHtmlCache
{
  private static readonly char[] InvalidPathChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

  public HtmlCache(
      IOptions<HtmlCacheOptions> options,
      ILogger<HtmlCache> logger,
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
    var baseFilename = UrlToSafeFilename(key, includeTimestamp: false);
    var baseWithoutExt = baseFilename.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
        ? baseFilename[..^5]
        : baseFilename;
    return $"{baseWithoutExt}-{ts}.html";
  }

  protected override long? ExtractTimestampFromFilename(string filename)
  {
    var match = Regex.Match(filename, @"-(\d+)\.html$", RegexOptions.IgnoreCase);
    if (match.Success && long.TryParse(match.Groups[1].Value, out var ts))
      return ts;
    return null;
  }

  protected override IReadOnlyList<string> FindCachedFiles(string key)
  {
    if (!Directory.Exists(StoreFolder))
      return [];

    var baseFilename = UrlToSafeFilename(key, includeTimestamp: false);
    var baseWithoutExt = baseFilename.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
        ? baseFilename[..^5]
        : baseFilename;
    var prefix = baseWithoutExt + "-";
    var list = new List<string>();

    foreach (var filePath in Directory.EnumerateFiles(StoreFolder, "*.html"))
    {
      var fileName = Path.GetFileName(filePath);
      if (fileName.StartsWith(prefix, StringComparison.Ordinal) &&
          fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        list.Add(filePath);
    }

    return list;
  }

  protected override async ValueTask<object?> ReadFileAsync(string filePath, CancellationToken cancellationToken)
  {
    return await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
  }

  protected override async ValueTask WriteFileAsync(string filePath, object data, CancellationToken cancellationToken)
  {
    var text = (string)data;
    await File.WriteAllTextAsync(filePath, text, cancellationToken).ConfigureAwait(false);
  }

  public new async Task<string?> LoadAsync(string url, CancellationToken cancellationToken = default)
  {
    var result = await base.LoadAsync(url, cancellationToken).ConfigureAwait(false);
    return result as string;
  }

  public Task SaveAsync(string url, string html, CancellationToken cancellationToken = default) =>
      base.SaveAsync(url, html, cancellationToken);

  public new Task<int> ClearAsync(string url, CancellationToken cancellationToken = default) =>
      base.ClearAsync(url, cancellationToken);

  private static string UrlToSafeFilename(string url, bool includeTimestamp)
  {
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
      uri = new Uri("https://unknown/" + url.TrimStart('/'));

    var path = uri.Host + uri.AbsolutePath;

    if (!string.IsNullOrEmpty(uri.Query))
      path += uri.Query;

    if (!string.IsNullOrEmpty(uri.Fragment))
      path += uri.Fragment;

    foreach (var c in InvalidPathChars)
      path = path.Replace(c, '_');

    var filename = Uri.EscapeDataString(path);
    foreach (var c in InvalidPathChars)
      filename = filename.Replace(c, '_');

    if (includeTimestamp)
      filename += "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    if (!filename.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
      filename += ".html";

    return filename;
  }

  private static string ResolveStoreFolder(string storeFolder, IHostEnvironment env)
  {
    if (Path.IsPathRooted(storeFolder))
      return storeFolder;
    return Path.Combine(env.ContentRootPath, storeFolder);
  }
}
