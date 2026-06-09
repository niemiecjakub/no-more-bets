using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure.Scraping.External.SoccerData;

internal static class SoccerDataMatchEventTypeMapper
{
  public static MatchEventType? Map(string? eventType) =>
    eventType?.Trim().ToLowerInvariant() switch
    {
      "goal" => MatchEventType.Goal,
      "own_goal" => MatchEventType.OwnGoal,
      "penalty_goal" => MatchEventType.PenaltyGoal,
      "red_card" => MatchEventType.RedCard,
      "yellow_card" => MatchEventType.YellowCard,
      "yellow_red_card" => MatchEventType.YellowRedCard,
      _ => null
    };
}
