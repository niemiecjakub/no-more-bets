using System.Text;
using System.Text.Json;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Domain.Matches;

public class MatchAnalysis : IDocumentChunkSource
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  public const string ResearchCode = "Research";
  public const string StructuredResearchCode = "StructuredResearch";

  public int Id { get; set; }
  public int MatchId { get; set; }
  public int? AgentSessionId { get; set; }
  public string Code { get; set; } = null!;
  public string Content { get; set; } = null!;

  public Match Match { get; set; } = null!;
  public AgentSession? AgentSession { get; set; }

  public static MatchAnalysis CreateStructuredResearch(int matchId, int? agentSessionId, MatchResearchOutput output) =>
    new()
    {
      MatchId = matchId,
      AgentSessionId = agentSessionId,
      Code = StructuredResearchCode,
      Content = JsonSerializer.Serialize(output, JsonOptions),
    };

  public static string FormatResearchOutput(MatchResearchOutput research)
  {
    var sb = new StringBuilder();
    sb.Append(research.MatchOverview);

    if (research.KeyPoints.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine();
      sb.AppendLine("Key points:");
      foreach (var point in research.KeyPoints)
      {
        sb.AppendLine($"- {point}");
      }
    }

    if (research.RisksAndUnknowns.Count > 0)
    {
      sb.AppendLine();
      sb.AppendLine("Risks and unknowns:");
      foreach (var risk in research.RisksAndUnknowns)
      {
        sb.AppendLine($"- {risk}");
      }
    }

    return sb.ToString();
  }

  public string? GetAgentResearch()
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

  private StructuredMatchAnalysis? GetAnalysis()
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

  private MatchResearchOutput? GetStructuredResearch()
  {
    if (Code != StructuredResearchCode || string.IsNullOrEmpty(Content))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize<MatchResearchOutput>(Content, JsonOptions);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  public StructuredMatchAnalysis? TryGetStructuredAnalysis() => GetAnalysis();

  public MatchResearchOutput? TryGetAgentResearchOutput()
  {
    var structured = GetStructuredResearch();
    if (structured != null)
    {
      return structured;
    }

    var legacyText = GetAgentResearch();
    if (string.IsNullOrEmpty(legacyText))
    {
      return null;
    }

    return new MatchResearchOutput
    {
      MatchOverview = legacyText,
      KeyPoints = [],
      RisksAndUnknowns = [],
    };
  }

  public string? BuildEmbeddingText()
  {
    var research = TryGetAgentResearchOutput();
    if (research is null)
      return null;

    var body = FormatResearchOutput(research).Trim();
    return string.IsNullOrWhiteSpace(body) ? null : body;
  }

  public DocumentChunkMetadata BuildMetadata() => Match.BuildMetadata();
}

public sealed record ResearchText(string Text);