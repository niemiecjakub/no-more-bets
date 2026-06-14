using System.Text.Json;

namespace NoMoreBets.Application.AgentSessions.ToolCallDisplay;

internal static class WebSearchToolMetadataParser
{
  internal sealed record WebSearchSource(string? Title, string? Hostname, string? Url);

  public static IReadOnlyList<WebSearchSource> Parse(string? metadataJson)
  {
    if (string.IsNullOrWhiteSpace(metadataJson))
      return [];

    try
    {
      using var document = JsonDocument.Parse(metadataJson);
      if (!document.RootElement.TryGetProperty("sources", out var sourcesElement)
        || sourcesElement.ValueKind != JsonValueKind.Array)
      {
        return [];
      }

      var sources = new List<WebSearchSource>();
      foreach (var item in sourcesElement.EnumerateArray())
      {
        if (item.ValueKind != JsonValueKind.Object)
          continue;

        var title = ReadStringProperty(item, "title");
        var url = ReadStringProperty(item, "url");
        var hostname = ReadStringProperty(item, "hostname");

        if (string.IsNullOrWhiteSpace(title)
          && string.IsNullOrWhiteSpace(url)
          && string.IsNullOrWhiteSpace(hostname))
        {
          continue;
        }

        sources.Add(new WebSearchSource(title, hostname, url));
      }

      return sources;
    }
    catch (JsonException)
    {
      return [];
    }
  }

  private static string? ReadStringProperty(JsonElement element, string propertyName)
  {
    if (!element.TryGetProperty(propertyName, out var property)
      || property.ValueKind != JsonValueKind.String)
    {
      return null;
    }

    var value = property.GetString()?.Trim();
    return string.IsNullOrWhiteSpace(value) ? null : value;
  }
}
