using System.Text.Json.Serialization;

namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>
/// Per-team block in match analysis: name, lineup, and optional league statistics.
/// </summary>
public record MatchTeamData
{
  public required string Name { get; init; }
  public required TeamLineupData Lineup { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public TeamLeagueStats? LeagueStatistics { get; init; }
}
