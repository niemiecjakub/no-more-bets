namespace NoMoreBets.Infrastructure.AI.Plugins.Models;

using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

public record TeamLineupResult(LineupType LineupType, IReadOnlyList<PlayerInLineup> Players);

public record MatchLineupResult(TeamLineupResult Home, TeamLineupResult Away);

public record TeamInjuriesResult(IReadOnlyList<InjuryEntry> Injuries);

public record MatchInjuriesResult(TeamInjuriesResult Home, TeamInjuriesResult Away);

public record RecentMatch(int MatchId, string Opponent, string Score, string Result, DateTime Date);

/// <summary>Single odds value over a time range; emitted only when odds change.</summary>
public record OddsSegmentResult(double Odds, DateTime StartTime, DateTime? EndTime);

/// <summary>Single option in betting odds history with odds segments (only when value changes).</summary>
public record OddsHistoryOptionResult(string Label, IReadOnlyList<OddsSegmentResult> Segments);

/// <summary>One event type section in betting odds history.</summary>
public record BettingOddsHistorySectionResult(int EventTypeId, string EventTypeName, string? Title, IReadOnlyList<OddsHistoryOptionResult> Options);

/// <summary>Result of GetMatchBettingOddsHistory: match ID and sections per event type.</summary>
public record MatchBettingOddsHistoryResult(int MatchId, IReadOnlyList<BettingOddsHistorySectionResult> Sections);

/// <summary>Internal accumulator for building betting odds history by event type.</summary>
internal sealed class EventTypeOddsAccumulator
{
  public string EventTypeName { get; set; } = "";
  public string? Title { get; set; }
  public List<string> OptionOrder { get; set; } = new();
  public Dictionary<string, List<(double Odds, DateTime At)>> OddsByLabel { get; set; } = new(StringComparer.Ordinal);
}
