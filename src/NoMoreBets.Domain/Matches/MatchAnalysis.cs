using NoMoreBets.Domain.Matches.Dto;
using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public class MatchAnalysis
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public int Id { get; set; }
  public int MatchId { get; set; }
  public string Code { get; set; } = null!;
  public string Content { get; set; } = null!;

  public Match Match { get; set; } = null!;

  public StructuredMatchAnalysis? GetAnalysis() => string.IsNullOrEmpty(Content)
    ? null
    : JsonSerializer.Deserialize<StructuredMatchAnalysis>(Content, JsonOptions);

}
