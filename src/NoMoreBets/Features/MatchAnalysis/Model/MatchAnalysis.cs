using System.Text.Json.Serialization;

namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>
/// Comprehensive match analysis aggregating data from multiple sources
/// (Betclic, Rotowire, SoccerData, FotMob). All nested types are MatchAnalysis-owned.
/// </summary>
public record MatchAnalysis
{
    public required MatchInfo MatchInfo { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LineupData? Lineup { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeadToHeadData? HeadToHead { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MatchPreviewData? MatchPreview { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<BettingEventInfo>? BettingEvents { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FbrefTeamData? FbrefHome { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FbrefTeamData? FbrefAway { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MatchId { get; init; }
}
