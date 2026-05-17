using System.Text;
using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public class MatchPreview
{
  public int MatchId { get; set; }
  public string PreviewContentJson { get; set; } = null!;

  public Match Match { get; set; } = null!;

  public IReadOnlyCollection<PreviewContentItem> GetPreview() => JsonSerializer.Deserialize<IReadOnlyCollection<PreviewContentItem>>(PreviewContentJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? Array.Empty<PreviewContentItem>();

  public string BuildMarkdownPreview()
  {
    var previewItems = GetPreview();
    if (previewItems.Count == 0)
    {
      return "No preview available.";
    }

    var markdownBuilder = new StringBuilder();
    foreach (var item in previewItems)
    {
      if (item.Name.StartsWith("h"))
      {
        markdownBuilder.AppendLine($"## {item.Content}");
      }
      else
      {
        markdownBuilder.AppendLine(item.Content);
      }
      markdownBuilder.AppendLine();
    }
    return markdownBuilder.ToString();
  }
}

public record PreviewContentItem
{
  public string Name { get; init; } = string.Empty;
  public string Content { get; init; } = string.Empty;
}

