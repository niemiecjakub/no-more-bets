using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure.AI.Plugins;
public class PluginConst
{
  public static readonly HashSet<BettingEventType> BettingOddsHistoryEventTypeWhitelist = new()
  {
    BettingEventType.OverUnderGoals,
    BettingEventType.DoubleChance,
    BettingEventType.BothTeamsToScore,
    BettingEventType.MatchResult,
    BettingEventType.Handicap,
    BettingEventType.ExactScore,
  };
}
