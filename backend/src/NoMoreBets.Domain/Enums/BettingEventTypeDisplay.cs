namespace NoMoreBets.Domain.Enums;

public static class BettingEventTypeDisplay
{
  /// <summary>
  /// Canonical UI order for odds history and similar surfaces (match page, AI tools).
  /// </summary>
  public static IReadOnlyList<BettingEventType> DisplayOrder { get; } =
  [
    BettingEventType.MatchResult,
    BettingEventType.DoubleChance,
    BettingEventType.BothTeamsToScore,
    BettingEventType.OverUnderGoals,
    BettingEventType.Handicap,
    BettingEventType.ExactScore,
  ];

  /// <summary>
  /// Sort key for ordering markets: known types use their index; unknown types sort last.
  /// </summary>
  public static int GetDisplayOrder(BettingEventType type)
  {
    for (var i = 0; i < DisplayOrder.Count; i++)
    {
      if (DisplayOrder[i] == type)
        return i;
    }

    return int.MaxValue;
  }

  public static string GetDisplayName(BettingEventType type) => type switch
  {
    BettingEventType.OverUnderGoals => "Over/Under Goals",
    BettingEventType.DoubleChance => "Double Chance",
    BettingEventType.BothTeamsToScore => "Both Teams to Score",
    BettingEventType.MatchResult => "Match Result (90 min)",
    BettingEventType.Handicap => "Handicap",
    BettingEventType.ExactScore => "Exact Score",
    _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown betting event type."),
  };
}
