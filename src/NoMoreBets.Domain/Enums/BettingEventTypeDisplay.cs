namespace NoMoreBets.Domain.Enums;

public static class BettingEventTypeDisplay
{
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
