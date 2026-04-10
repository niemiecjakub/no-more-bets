using NoMoreBets.Domain.Matches.Dto;
using System.Text.Json;

namespace NoMoreBets.Domain.Matches;

public class MatchAnalysis
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  public const string ResearchCode = "Research";

  public int Id { get; set; }
  public int MatchId { get; set; }
  public string Code { get; set; } = null!;
  public string Content { get; set; } = null!;

  public Match Match { get; set; } = null!;

  public string GetAgentResearch()
  {
    if (Code != ResearchCode || string.IsNullOrEmpty(Content))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<ResearchText>(Content, JsonOptions)?.Text ?? string.Empty;
    }
    catch (JsonException)
    {
      return string.Empty;
    }
  }

  public StructuredMatchAnalysis? GetAnalysis()
  {
    if (string.IsNullOrEmpty(Content))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<StructuredMatchAnalysis>(Content, JsonOptions);
    }
    catch (JsonException)
    {
      return null;
    }
  }
}

public sealed record ResearchText(string Text);