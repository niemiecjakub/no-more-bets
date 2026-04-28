namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>Display names for recurring job groups (used by the API and registration).</summary>
public static class JobGroups
{
  public const string MatchLifecycle = "Match Lifecycle";
  public const string PreKickoffSync = "Pre-Kickoff Data Sync";
  public const string Bankroll = "Bankroll";
  public const string LeagueData = "League Data";
  public const string BettingAgent = "Betting Agent";
  public const string BookmakerSync = "Bookmaker Sync";
  public const string ClubInsights = "Club Insights";
  public const string Lineups = "Lineups";
  public const string Results = "Results";
}
