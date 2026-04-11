using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search.SearchNews;

public sealed class SearchNewsOptions
{
  public int Count { get; init; } = 10;

  /// <summary>
  /// Optional time range for results (Bing-style freshness codes). Omit for no filter.
  /// <list type="bullet">
  /// <item><term>pd</term> — Last 24 hours: get breaking news and latest updates</item>
  /// <item><term>pw</term> — Last 7 days: track weekly news trends</item>
  /// <item><term>pm</term> — Last 31 days: monitor monthly developments</item>
  /// <item><term>py</term> — Last year: search annual news coverage</item>
  /// </list>
  /// </summary>
  [RegularExpression("^(pd|pw|pm|py)?$", ErrorMessage = "Freshness must be 'pd' (last 24 hours), 'pw' (last 7 days), 'pm' (last 31 days), 'py' (last year), or empty to omit.")]
  public string? Freshness { get; init; }

  public string? Country { get; init; }
  public bool ExtraSnippets { get; init; } = false;

  [DefaultValue("moderate")]
  [RegularExpression("off|moderate|strict", ErrorMessage = "Safesearch must be 'off', 'moderate', or 'strict'.")]
  public string Safesearch { get; init; } = "moderate";

  /// <summary>Builds a query dictionary for the news search API. Include <paramref name="q"/> to add the search query.</summary>
  public Dictionary<string, string?> ToQueryDictionary(string? q = null)
  {
    var count = Math.Clamp(Count, 1, 50);
    var d = new Dictionary<string, string?>
    {
      ["count"] = count.ToString(),
      ["freshness"] = Freshness,
      ["country"] = Country,
      ["safesearch"] = Safesearch,
      ["extra_snippets"] = ExtraSnippets ? "true" : "false"
    };
    if (!string.IsNullOrWhiteSpace(q))
      d["q"] = q;
    return d;
  }
}
