using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search.SearchBasic;

public sealed class SearchBasicOptions
{
  /// <summary>
  /// Optional Brave goggles identifier (e.g. "local" or custom).
  /// See Brave web search docs for available goggles.
  /// https://api-dashboard.search.brave.com/api-reference/web/search/get
  /// </summary>
  public string? Goggles { get; init; }

  /// <summary>
  /// Safesearch level: "off", "moderate", or "strict".
  /// Defaults to Brave's recommended "moderate".
  /// </summary>
  [DefaultValue("moderate")]
  [RegularExpression("off|moderate|strict")]
  public string Safesearch { get; init; } = "moderate";

  /// <summary>
  /// Optional result filter, e.g. "web", "news", "discussions".
  /// When null, Brave's default mix of results is returned.
  /// </summary>
  public string? ResultFilter { get; init; }

  /// <summary>Builds a query dictionary for the web search API. Include <paramref name="q"/> to add the search query.</summary>
  public Dictionary<string, string?> ToQueryDictionary(string? q = null)
  {
    var d = new Dictionary<string, string?>
    {
      ["goggles"] = Goggles,
      ["result_filter"] = ResultFilter,
      ["safesearch"] = Safesearch
    };
    if (!string.IsNullOrWhiteSpace(q))
      d["q"] = q;
    return d;
  }
}
