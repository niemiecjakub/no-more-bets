using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Search;
using NoMoreBets.Application.Search.SearchBasic;
using NoMoreBets.Application.Search.SearchLlmContext;
using NoMoreBets.Application.Search.SearchNews;

namespace NoMoreBets.Infrastructure.Search;

public sealed class BraveSearch : ISearchService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<BraveSearch> _logger;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public BraveSearch(HttpClient httpClient, ILogger<BraveSearch> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<SearchBasicResultDto> SearchBasicAsync(string q, SearchBasicOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchBasicOptions();
    var query = options.ToQueryDictionary(q);
    var web = await SendAsync<BraveWebSearchResponse>("/res/v1/web/search", query, cancellationToken).ConfigureAwait(false);
    return MapSearchResults(web);
  }

  public async Task<SearchNewsResultDto> SearchNewsAsync(string q, SearchNewsOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchNewsOptions();
    var query = options.ToQueryDictionary(q);
    var news = await SendAsync<BraveNewsSearchResponse>("/res/v1/news/search", query, cancellationToken).ConfigureAwait(false);
    return MapNewsResults(news);
  }

  public async Task<SearchLlmContextResultDto> SearchLlmContextAsync(string q, SearchLlmContextOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchLlmContextOptions();
    var query = options.ToQueryDictionary(q);
    var ctx = await SendAsync<BraveLlmContextResponse>("/res/v1/llm/context", query, cancellationToken).ConfigureAwait(false);
    return MapLlmContextResults(ctx);
  }

  private async Task<T> SendAsync<T>(string path, IDictionary<string, string?> queryParams, CancellationToken cancellationToken)
  {
    var filtered = new Dictionary<string, string?>();
    foreach (var kv in queryParams)
    {
      if (!string.IsNullOrWhiteSpace(kv.Value))
        filtered[kv.Key] = kv.Value;
    }

    var relativeUri = QueryHelpers.AddQueryString(path, filtered);

    using var response = await _httpClient.GetAsync(relativeUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      _logger.LogError(
        "Brave request failed for path {Path} with status code {StatusCode}. Query: {QuerySummary}. ResponseExcerpt: {ResponseExcerpt}",
        path,
        (int)response.StatusCode,
        BuildQuerySummary(filtered),
        TruncateForLogs(responseBody));
      throw new BraveSearchException($"Brave search request failed with status {(int)response.StatusCode}.");
    }

    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    if (result is null)
    {
      _logger.LogWarning(
        "Brave request succeeded for path {Path}, but response payload could not be deserialized to {ResponseType}. Query: {QuerySummary}",
        path,
        typeof(T).Name,
        BuildQuerySummary(filtered));
      throw new BraveSearchException("Brave search response body was empty or invalid.");
    }

    return result;
  }

  private SearchBasicResultDto MapSearchResults(BraveWebSearchResponse response)
  {
    if (response.Web.Results is null || response.Web.Results.Count == 0)
      return new SearchBasicResultDto();

    var items = new List<SearchBasicResultItemDto>();
    foreach (var item in response.Web.Results)
    {
      if (string.IsNullOrWhiteSpace(item.Url))
        continue;

      items.Add(new SearchBasicResultItemDto
      {
        Title = item.Title ?? string.Empty,
        Url = item.Url,
        Snippet = item.Description ?? string.Empty,
        Hostname = item.MetaUrl?.Hostname,
        DisplayUrlPath = item.MetaUrl?.Path,
        ThumbnailUrl = item.Thumbnail?.Src
      });
    }

    if (response.Web.Results.Count > 0 && items.Count == 0)
    {
      _logger.LogWarning(
        "Brave web response contained {InputCount} items but all were filtered out due to missing URLs.",
        response.Web.Results.Count);
    }

    return new SearchBasicResultDto { Items = items };
  }

  private SearchNewsResultDto MapNewsResults(BraveNewsSearchResponse response)
  {
    if (response.Results is null || response.Results.Count == 0)
      return new SearchNewsResultDto();

    var items = new List<SearchNewsArticleDto>();
    foreach (var item in response.Results)
    {
      if (string.IsNullOrWhiteSpace(item.Url))
        continue;

      DateTimeOffset? publishedAt = null;
      if (DateTimeOffset.TryParse(item.PageAge, out var parsed))
        publishedAt = parsed;

      items.Add(new SearchNewsArticleDto
      {
        Title = item.Title ?? string.Empty,
        Url = item.Url,
        Source = item.MetaUrl?.Hostname ?? string.Empty,
        PublishedAt = publishedAt,
        Snippet = item.Description ?? string.Empty,
        Age = item.Age,
        PageAge = publishedAt,
        Hostname = item.MetaUrl?.Hostname,
        ThumbnailUrl = item.Thumbnail?.Src,
        ExtraSnippets = item.ExtraSnippets ?? []
      });
    }

    if (response.Results.Count > 0 && items.Count == 0)
    {
      _logger.LogWarning(
        "Brave news response contained {InputCount} items but all were filtered out due to missing URLs.",
        response.Results.Count);
    }

    return new SearchNewsResultDto { Items = items };
  }

  private SearchLlmContextResultDto MapLlmContextResults(BraveLlmContextResponse response)
  {
    var generic = response.Grounding?.Generic;
    if (generic is null || generic.Count == 0)
      return new SearchLlmContextResultDto();

    var sources = response.Sources ?? new Dictionary<string, BraveLlmContextSource>();
    var items = new List<SearchLlmContextItemDto>();

    foreach (var item in generic)
    {
      if (item.Snippets is null || item.Snippets.Count == 0)
        continue;

      var snippets = item.Snippets.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
      if (snippets.Count == 0)
        continue;

      string? hostname = null;
      string? age = null;
      if (sources.TryGetValue(item.Url, out var source) && source is not null)
      {
        if (!string.IsNullOrWhiteSpace(source.Hostname))
          hostname = source.Hostname;
        if (source.Age is { Count: > 0 })
          age = source.Age[^1];
      }

      items.Add(new SearchLlmContextItemDto
      {
        Snippets = snippets,
        Url = item.Url,
        Title = item.Title,
        Hostname = hostname,
        Age = age
      });
    }

    if (generic.Count > 0 && items.Count == 0)
    {
      _logger.LogWarning(
        "Brave LLM context response contained {InputCount} grounding items but none produced usable snippets.",
        generic.Count);
    }

    return new SearchLlmContextResultDto { Items = items };
  }

  private static string BuildQuerySummary(IReadOnlyDictionary<string, string?> queryParams)
  {
    queryParams.TryGetValue("q", out var q);
    queryParams.TryGetValue("count", out var count);
    queryParams.TryGetValue("offset", out var offset);

    var trimmedQuery = string.IsNullOrWhiteSpace(q) ? "<empty>" : q.Trim();
    if (trimmedQuery.Length > 120)
      trimmedQuery = trimmedQuery[..120] + "...";

    return $"q='{trimmedQuery}', count='{count ?? "<null>"}', offset='{offset ?? "<null>"}'";
  }

  private static string TruncateForLogs(string? input, int maxLength = 400)
  {
    if (string.IsNullOrWhiteSpace(input))
      return "<empty>";

    return input.Length <= maxLength
      ? input
      : input[..maxLength] + "...";
  }
}

public class BraveSearchException : Exception
{
  public BraveSearchException(string message, Exception? innerException = null) : base(message, innerException) { }
}

