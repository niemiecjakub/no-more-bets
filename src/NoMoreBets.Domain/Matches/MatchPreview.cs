using System.Text.Json;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Domain.Matches;

public class MatchPreview
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int MatchId { get; set; }
  public string PreviewContentJson { get; set; } = null!;

  public Match Match { get; set; } = null!;

  public IReadOnlyList<PreviewContentItem> GetPreviewContent() =>
    JsonSerializer.Deserialize<IReadOnlyList<PreviewContentItem>>(PreviewContentJson, JsonOptions) ?? [];
}
