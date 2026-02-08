using System.Text.Json.Serialization;

namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Expected goals and points for a team (MatchAnalysis-owned, for game analysis).</summary>
public record TeamXgData
{
  public double Xg { get; init; }
  public double Xga { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? XgDiff { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? XgaDiff { get; init; }
}
