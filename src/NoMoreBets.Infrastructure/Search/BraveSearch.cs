using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Search;

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

  public async Task<SearchResultDto> SearchAsync(string q, SearchOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchOptions();

    var query = new Dictionary<string, string?>
    {
      ["q"] = q,
      ["goggles"] = options.Goggles,
      ["result_filter"] = options.ResultFilter,
      ["safesearch"] = options.Safesearch
    };

    var web = await SendAsync<BraveWebSearchResponse>("/res/v1/web/search", query, cancellationToken).ConfigureAwait(false);
    return MapSearchResults(web);
  }

  public async Task<SearchNewsResultDto> SearchNewsAsync(string q, SearchNewsOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchNewsOptions();

    var query = new Dictionary<string, string?>
    {
      ["q"] = q,
      ["freshness"] = options.Freshness,
      ["country"] = options.Country,
      ["extra_snippets"] = options.ExtraSnippets ? "true" : "false"
    };

    var news = await SendAsync<BraveNewsSearchResponse>("/res/v1/news/search", query, cancellationToken).ConfigureAwait(false);
    return MapNewsResults(news);
  }

  public async Task<SearchLlmContextResultDto> SearchLlmContextAsync(string q, SearchLlmContextOptions options, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(q))
      throw new ArgumentException("Query 'q' is required.", nameof(q));

    options ??= new SearchLlmContextOptions();

    var maxTokens = Math.Clamp(options.MaximumNumberOfTokens, 128, 4096);
    var count = Math.Clamp(options.Count, 1, 50);

    var query = new Dictionary<string, string?>
    {
      ["q"] = q,
      ["maximum_number_of_tokens"] = maxTokens.ToString(),
      ["count"] = count.ToString(),
      ["context_threshold_mode"] = options.ContextThresholdMode
    };

    var ctx = await SendAsync<BraveLlmContextResponse>("/res/v1/llm/context", query, cancellationToken).ConfigureAwait(false);
    return MapLlmContextResults(ctx);
  }

  private async Task<T> SendAsync<T>(string path, IDictionary<string, string?> queryParams, CancellationToken cancellationToken)
  {
    // 1. Build the relative URI with query string
    var filtered = new Dictionary<string, string?>();
    foreach (var kv in queryParams)
    {
      if (!string.IsNullOrWhiteSpace(kv.Value))
        filtered[kv.Key] = kv.Value;
    }

    var relativeUri = QueryHelpers.AddQueryString(path, filtered);

    using var response = await _httpClient.GetAsync(relativeUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

    try
    {
      response.EnsureSuccessStatusCode();
    }
    catch (Exception ex)
    {
      throw new BraveSearchException($"Brave search request failed with status {(int)response.StatusCode}.", ex);
    }

    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
    if (result is null)
      throw new BraveSearchException("Brave search response body was empty or invalid.");
    return result;
  }

  private static SearchResultDto MapSearchResults(BraveWebSearchResponse response)
  {
    if (response.Web.Results is null || response.Web.Results.Count == 0)
      return new SearchResultDto();

    var items = new List<SearchResultItemDto>();
    foreach (var item in response.Web.Results)
    {
      if (string.IsNullOrWhiteSpace(item.Url))
        continue;

      items.Add(new SearchResultItemDto
      {
        Title = item.Title ?? string.Empty,
        Url = item.Url,
        Snippet = item.Description ?? string.Empty,
        Hostname = item.MetaUrl?.Hostname,
        DisplayUrlPath = item.MetaUrl?.Path,
        ThumbnailUrl = item.Thumbnail?.Src
      });
    }

    return new SearchResultDto { Items = items };
  }

  private static SearchNewsResultDto MapNewsResults(BraveNewsSearchResponse response)
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

    return new SearchNewsResultDto { Items = items };
  }

  private static SearchLlmContextResultDto MapLlmContextResults(BraveLlmContextResponse response)
  {
    if (response.Results is null || response.Results.Count == 0)
      return new SearchLlmContextResultDto();

    var items = new List<SearchLlmContextItemDto>();
    foreach (var item in response.Results)
    {
      if (string.IsNullOrWhiteSpace(item.Content))
        continue;

      items.Add(new SearchLlmContextItemDto
      {
        Text = item.Content,
        Url = item.Url,
        TokenCount = item.Tokens,
        Title = item.Title,
        Score = item.Score,
        SourceType = item.SourceType
      });
    }

    return new SearchLlmContextResultDto { Items = items };
  }
}

public class BraveSearchException : Exception
{
  public BraveSearchException(string message, Exception? innerException = null) : base(message, innerException) { }
}

