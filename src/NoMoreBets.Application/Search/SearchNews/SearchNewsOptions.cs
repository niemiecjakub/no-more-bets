using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NoMoreBets.Application.Search.SearchNews;

public sealed class SearchNewsOptions
{
  public int Count { get; init; } = 10;
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
