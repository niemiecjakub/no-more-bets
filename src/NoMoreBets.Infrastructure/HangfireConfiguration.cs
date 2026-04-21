using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Infrastructure.BackgroundJobs;

namespace NoMoreBets.Infrastructure;
public static class HangfireConfiguration
{
  public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    services.AddHangfire(config => config
      .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
      .UseSimpleAssemblyNameTypeSerializer()
      .UseIgnoredAssemblyVersionTypeResolver()
      .UseRecommendedSerializerSettings()
      .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

    services.AddHangfireServer(options => options.WorkerCount = 3);

    return services;
  }

  public static void UseRecurringJobs()
  {
    // Runs hourly at minute 0 (first run 00:00)
    RecurringJob.AddOrUpdate<MatchLifecycleJobService>(
        "close-starting-soon-matches",
        jobService => jobService.CloseStartingSoonMatches(),
        "0 * * * *");

    // Runs once per day at 01:00
    RecurringJob.AddOrUpdate<PreKickoffDataSyncJobService>(
      "get-soccerdata-upcoming-matches",
      jobService => jobService.GetUpcommingSoccerdataMatchesForAllLeagues(),
      "0 1 * * *");

    // Runs once per day at 02:00 – schedule head-to-head refresh jobs for upcoming matches
    RecurringJob.AddOrUpdate<PreKickoffDataSyncJobService>(
      "refresh-head2head-upcoming-matches",
      jobService => jobService.ScheduleRefreshHead2HeadForUpcomingMatches(),
      "0 2 * * *");

    // Runs once per day at 02:01 – schedule preview fetch jobs for upcoming matches missing a preview
    RecurringJob.AddOrUpdate<PreKickoffDataSyncJobService>(
      "get-missing-match-previews",
      jobService => jobService.ScheduleMissingPreviewJobsForUpcomingMatches(),
      "1 2 * * *");

    // Runs once per day at 03:00 UTC — applies salary when today is payday (last day of month)
    RecurringJob.AddOrUpdate<BankrollJobService>(
        "apply-payday-if-due",
        jobService => jobService.ApplyPaydayIfDue(),
        "0 3 * * *");

    // Runs once per day at 04:00
    RecurringJob.AddOrUpdate<LeagueTableJobService>(
        "get-league-table",
        jobService => jobService.GetLeagueTable(),
        "0 4 * * *");

    // Runs once per day at 04:15 UTC — agent prunes stale fixture-specific memories
    RecurringJob.AddOrUpdate<MemoryCleanupCronService>(
        "betting-agent-memory-cleanup",
        s => s.RunAsync(),
        "15 4 * * *");

    // Runs once per day at 05:00
    RecurringJob.AddOrUpdate<BookmakerListingSyncJobService>(
        "get-upcoming-betclic-games",
        jobService => jobService.GetBetclicGames(),
        "0 5 * * *");

    // Runs once per day at 08:00
    RecurringJob.AddOrUpdate<ClubDailyBriefJobService>(
        "update-clubs-overview",
        jobService => jobService.UpdateClubOverview(),
        "0 8 * * *");

    // Runs once per day at 09:00
    RecurringJob.AddOrUpdate<LineupJobService>(
        "get-lineups",
        jobService => jobService.GetLineups(),
        "0 9 * * *");

    // Runs once per day at 11:00
    RecurringJob.AddOrUpdate<BookmakerListingSyncJobService>(
        "get-betting-odds",
        jobService => jobService.ScheduleBettingOddsJob(),
        "0 11 * * *");

    // Runs once per day at 10:00 — portfolio internet research for upcoming fixtures (before per-match research)
    RecurringJob.AddOrUpdate<UpcomingMatchesInternetResearchCronService>(
        "betting-agent-upcoming-internet-research",
        s => s.RunAsync(),
        "0 10 * * *");

    // Runs once per day at 11:30
    RecurringJob.AddOrUpdate<BettingAgentCronService>(
        "betting-agent-research",
        s => s.RunResearchScheduleAsync(),
        "30 11 * * *");

    // Runs daily at 13:00
    RecurringJob.AddOrUpdate<BettingAgentCronService>(
        "betting-agent-execution",
        s => s.RunBettingExecutionAsync(),
        "0 13 * * *");

    // Runs once per day at 23:00
    RecurringJob.AddOrUpdate<FinishedMatchScoreJobService>(
        "fill-missing-finished-match-scores",
        jobService => jobService.FillMissingFinishedMatchScoresFromSoccerData(),
        "0 23 * * *");

    // Runs daily at 23:40
    RecurringJob.AddOrUpdate<BettingAgentCronService>(
        "betting-agent-reflection",
        s => s.RunReflectionAsync(),
        "40 23 * * *");
  }
}
