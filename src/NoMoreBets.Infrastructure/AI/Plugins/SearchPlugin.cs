using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using SearchLlmContextOptions = NoMoreBets.Application.Search.SearchLlmContext.SearchLlmContextOptions;
using SearchNewsOptions = NoMoreBets.Application.Search.SearchNews.SearchNewsOptions;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class SearchPlugin
{
  private readonly ISearchService _searchService;

  public SearchPlugin(ISearchService searchService)
  {
    _searchService = searchService;
  }

  [KernelFunction("SearchNews")]
  [Description("Search for recent news articles and current events.")]
  public async Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(
    [Description("The specific news topic or keywords to search for.")]
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    var src = await _searchService.SearchNewsAsync(query, new SearchNewsOptions()
    {
      Count = 5,
      Freshness = freshness,
      Country = "GB",
      ExtraSnippets = true
    }, cancellationToken).ConfigureAwait(false);
    return src.Items
      .OrderByDescending(item => item.PublishedAt ?? DateTimeOffset.MinValue)
      .Select(item => new SearchNewsArticleDto(
        Title: item.Title,
        Source: item.Source,
        Snippets: item.ExtraSnippets.Count > 0 ? [item.Snippet, .. item.ExtraSnippets] : [item.Snippet],
        Age: item.Age)).ToList();
  }

  [KernelFunction("GetWebGrounding")]
  [Description("Retrieves high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context for a complex question, or summarizing a specific topic.")]
  public async Task<IReadOnlyList<SearchLlmContextItemDto>> GetWebGroundingAsync(
    [Description("The detailed search query or question to gather context for.")]
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    var src = await _searchService.SearchLlmContextAsync(query, new SearchLlmContextOptions()
    {
      Count = 5,
      Freshness = freshness,
    }, cancellationToken).ConfigureAwait(false);
    return src.Items.Select(item => new SearchLlmContextItemDto(
        Snippets: item.Snippets,
        Title: item.Title,
        Hostname: item.Hostname,
      Age: item.Age)).ToList();
  }
}
