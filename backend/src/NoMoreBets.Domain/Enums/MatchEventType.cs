 namespace NoMoreBets.Domain.Enums;

public enum MatchEventType
{
  Goal = 1,
  Assist = 2,
  OwnGoal = 3,
  PenaltyGoal = 4,
  RedCard = 5,
  YellowCard = 6,
  YellowRedCard = 7,
  SubstitutionIn = 8,
  SubstitutionOut = 9
}

public static class MatchEventTypeExtensions
{
  private static readonly MatchEventType[] EmbeddingEventTypes =
  [
    MatchEventType.Goal,
    MatchEventType.Assist,
    MatchEventType.OwnGoal,
    MatchEventType.PenaltyGoal,
    MatchEventType.RedCard,
    MatchEventType.YellowCard,
    MatchEventType.YellowRedCard
  ];

  public static bool IsEmbeddingEventType(this MatchEventType type) =>
    Array.IndexOf(EmbeddingEventTypes, type) >= 0;
}
