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
}
