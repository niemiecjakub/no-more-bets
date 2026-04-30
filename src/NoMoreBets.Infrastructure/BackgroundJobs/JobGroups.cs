namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>Display names for recurring job groups (used by the API and registration).</summary>
public static class JobGroups
{
  public const string MatchLifecycle = "Match Lifecycle";
  public const string DataPreparation = "Data Preparation";
  public const string Bankroll = "Bankroll";
  public const string Betting = "Betting";
  public const string Maintenance = "Maintenance";
  public const string Reflection = "Reflection";
  public const string Research = "Research";

  private static readonly IReadOnlyDictionary<string, int> GroupOrderByName = new Dictionary<string, int>(StringComparer.Ordinal)
  {
    [MatchLifecycle] = 0,
    [DataPreparation] = 1,
    [Maintenance] = 2,
    [Research] = 3,
    [Betting] = 4,
    [Reflection] = 5,
    [Bankroll] = 6
  };

  public static int GetOrder(string group) =>
    GroupOrderByName.TryGetValue(group, out var order) ? order : int.MaxValue;
}
