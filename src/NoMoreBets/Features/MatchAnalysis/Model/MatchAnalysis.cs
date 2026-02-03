using System.Text.Json.Serialization;

namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>
/// Comprehensive match analysis aggregating data from multiple sources
/// (Betclic, Rotowire, SoccerData, FotMob). All nested types are MatchAnalysis-owned.
/// </summary>
public record MatchAnalysis
{
  public required string Game { get; init; }
  public required DateTime Date { get; init; }
  public required MatchTeamData HomeTeam { get; init; }
  public required MatchTeamData AwayTeam { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HeadToHeadData? HeadToHead { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<PreviewContentItem>? Preview { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<BettingEventInfo>? Betting { get; init; }
}
