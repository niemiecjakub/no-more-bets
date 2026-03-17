using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search.SearchNews;

public sealed class SearchNewsOptions
{
  /// <summary>
  /// Freshness window for news results. Examples: "day", "week", "month".
  /// See Brave news search docs.
  /// https://api-dashboard.search.brave.com/api-reference/news/news_search/get
  /// </summary>
  [RegularExpression("day|week|month", ErrorMessage = "freshness must be 'day', 'week', or 'month'.")]
  public string? Freshness { get; init; }

  /// <summary>
  /// Optional 2-letter country code (e.g. "US", "GB") that biases news sources.
  /// </summary>
  [StringLength(2, MinimumLength = 2)]
  public string? Country { get; init; }

  /// <summary>
  /// Whether to include extra_snippets (longer or multiple snippets).
  /// Defaults to false.
  /// </summary>
  [DefaultValue(false)]
  public bool ExtraSnippets { get; init; } = false;
}
