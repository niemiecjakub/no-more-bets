using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search;

public sealed class SearchLlmContextOptions
{
  /// <summary>
  /// Maximum tokens for the context response. Brave examples typically use
  /// ranges like 512–4096. Default is 2048.
  /// https://api-dashboard.search.brave.com/api-reference/summarizer/llm_context/get
  /// </summary>
  [DefaultValue(2048)]
  [Range(128, 4096)]
  public int MaximumNumberOfTokens { get; init; } = 2048;

  /// <summary>
  /// Number of context chunks/items to return. Default is 10.
  /// </summary>
  [DefaultValue(10)]
  [Range(1, 50)]
  public int Count { get; init; } = 10;

  /// <summary>
  /// Threshold mode controlling how aggressively results are filtered,
  /// e.g. "strict", "balanced", or "relaxed". Null uses Brave defaults.
  /// </summary>
  public string? ContextThresholdMode { get; init; }
}

