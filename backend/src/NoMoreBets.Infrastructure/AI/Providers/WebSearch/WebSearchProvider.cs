using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using SearchLlmContextOptions = NoMoreBets.Application.Search.SearchLlmContext.SearchLlmContextOptions;
using SearchNewsOptions = NoMoreBets.Application.Search.SearchNews.SearchNewsOptions;

namespace NoMoreBets.Infrastructure.AI.Providers.WebSearch;

public sealed class WebSearchProvider : AIContextProvider
{
  private const string SearchNewsToolName = "WebSearch_SearchNews";
  private const string GetWebGroundingToolName = "WebSearch_GetWebGrounding";

  private static readonly string Instructions =
      $$"""
        # Web Search
        You have access to web search.

        Use these tools to search the web:
        - Use {{SearchNewsToolName}} to search for recent news articles and current events.
        - Use {{GetWebGroundingToolName}} to retrieve high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context for a complex question, or summarizing a specific topic.
        """;

  private readonly ISearchService _searchService;
  private readonly ILogger<WebSearchProvider> _logger;

  public WebSearchProvider(ISearchService searchService, ILogger<WebSearchProvider>? logger = null)
  {
    _searchService = searchService;
    _logger = logger ?? NullLogger<WebSearchProvider>.Instance;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools = CreateTools(),
    };

    return ValueTask.FromResult(aiContext);
  }

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        SearchNewsAsync,
        new AIFunctionFactoryOptions
        {
          Name = SearchNewsToolName,
          Description = "Search for recent news articles and current events.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        GetWebGroundingAsync,
        new AIFunctionFactoryOptions
        {
          Name = GetWebGroundingToolName,
          Description = "Retrieves high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context for a complex question, or summarizing a specific topic.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }

  private async Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(
    [Description("The specific news topic or keywords to search for.")]
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var src = await _searchService.SearchNewsAsync(query, new SearchNewsOptions()
      {
        Count = 3,
        Freshness = freshness,
        Country = "GB",
        ExtraSnippets = true
      }, cancellationToken).ConfigureAwait(false);
      if (src.Items.Count == 0)
      {
        _logger.LogWarning("SearchNews returned no items for query {Query} and freshness {Freshness}.", query, freshness);
      }

      return src.Items
        .OrderByDescending(item => item.PublishedAt ?? DateTimeOffset.MinValue)
        .Select(item => new SearchNewsArticleDto(
          Title: item.Title,
          Source: item.Source,
          Snippets: item.ExtraSnippets.Count > 0 ? [item.Snippet, .. item.ExtraSnippets] : [item.Snippet],
          Age: item.Age)).ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "SearchNews failed for query {Query}.", query);
      throw;
    }
  }

  private async Task<SearchLlmContextItemDto> GetWebGroundingAsync(
    [Description("The detailed search query or question to gather context for.")]
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var src = await _searchService.SearchLlmContextAsync(query, new SearchLlmContextOptions()
      {
        Count = 1,
        Freshness = freshness,
      }, cancellationToken).ConfigureAwait(false);
      var firstItem = src.Items.FirstOrDefault();
      if (firstItem is null)
      {
        _logger.LogWarning("GetWebGrounding returned no items for query {Query} and freshness {Freshness}.", query, freshness);
        throw new InvalidOperationException("GetWebGrounding returned no items.");
      }

      return new SearchLlmContextItemDto(
        Snippets: firstItem.Snippets,
        Title: firstItem.Title,
        Hostname: firstItem.Hostname,
        Age: firstItem.Age);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "GetWebGrounding failed for query {Query}.", query);
      throw;
    }
  }
}
