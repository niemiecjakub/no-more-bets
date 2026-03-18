using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search.SearchLlmContext;

public sealed class SearchLlmContextOptions
{
  [DefaultValue(20)]
  [Range(1, 50)]
  public int Count { get; init; } = 20;

  [DefaultValue(8192)]
  [Range(1024, 32768)]
  public int MaximumNumberOfTokens { get; init; } = 8192;

  /// <summary>Mode to determine the threshold for including content in context. Possible values: disabled, strict, lenient, balanced. Default is balanced.</summary>
  [DefaultValue("balanced")]
  [RegularExpression("disabled|strict|lenient|balanced", ErrorMessage = "ContextThresholdMode must be 'disabled', 'strict', 'lenient', or 'balanced'.")]
  public string ContextThresholdMode { get; init; } = "balanced";

  /// <summary>Optional 2-letter country code (e.g. "US", "GB") that biases results.</summary>
  public string? Country { get; init; }

  /// <summary>Maximum number of URLs to include in the context.</summary>
  public int? MaximumNumberOfUrls { get; init; }

  /// <summary>Maximum number of snippets to return. Clamped to 1–100.</summary>
  [DefaultValue(50)]
  [Range(1, 100)]
  public int MaximumNumberOfSnippets { get; init; } = 50;

  /// <summary>Maximum number of tokens to include per URL. Clamped to 512–8192. Default 4096.</summary>
  [DefaultValue(4096)]
  [Range(512, 8192)]
  public int MaximumNumberOfTokensPerUrl { get; init; } = 4096;

  /// <summary>Maximum number of snippets to include per URL. Clamped to 1–100. Default 50.</summary>
  [DefaultValue(50)]
  [Range(1, 100)]
  public int MaximumNumberOfSnippetsPerUrl { get; init; } = 50;

  public string? Freshness { get; init; }

  /// <summary>When set, enables local/metadata filtering. Nullable.</summary>
  public bool? EnableLocal { get; init; }

  /// <summary>Builds a query dictionary for the LLM context API. Include <paramref name="q"/> to add the search query.</summary>
  public Dictionary<string, string?> ToQueryDictionary(string? q = null)
  {
    var maxTokens = Math.Clamp(MaximumNumberOfTokens, 1024, 32768);
    var count = Math.Clamp(Count, 1, 50);
    var maxSnippets = Math.Clamp(MaximumNumberOfSnippets, 1, 100);
    var maxTokensPerUrl = Math.Clamp(MaximumNumberOfTokensPerUrl, 512, 8192);
    var maxSnippetsPerUrl = Math.Clamp(MaximumNumberOfSnippetsPerUrl, 1, 100);
    var d = new Dictionary<string, string?>
    {
      ["maximum_number_of_tokens"] = maxTokens.ToString(),
      ["count"] = count.ToString(),
      ["maximum_number_of_snippets"] = maxSnippets.ToString(),
      ["maximum_number_of_tokens_per_url"] = maxTokensPerUrl.ToString(),
      ["maximum_number_of_snippets_per_url"] = maxSnippetsPerUrl.ToString(),
      ["context_threshold_mode"] = ContextThresholdMode,
      ["country"] = Country,
      ["maximum_number_of_urls"] = MaximumNumberOfUrls?.ToString(),
      ["freshness"] = Freshness,
      ["enable_local"] = EnableLocal.HasValue ? (EnableLocal.Value ? "true" : "false") : null
    };
    if (!string.IsNullOrWhiteSpace(q))
      d["q"] = q;
    return d;
  }
}
