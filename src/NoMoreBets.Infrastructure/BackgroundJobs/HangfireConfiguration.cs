using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
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

    services.AddSingleton<RecurringJobRegistry>();

    services.AddHangfire(config => config
      .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
      .UseSimpleAssemblyNameTypeSerializer()
      .UseIgnoredAssemblyVersionTypeResolver()
      .UseRecommendedSerializerSettings()
      .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

    services.AddHangfireServer(options => options.WorkerCount = 3);

    return services;
  }

  public static IApplicationBuilder UseRecurringJobs(this IApplicationBuilder app)
  {
    var registry = app.ApplicationServices.GetRequiredService<RecurringJobRegistry>();
    RegisterRecurringJobs(registry);
    return app;
  }

  private static void RegisterRecurringJobs(RecurringJobRegistry registry)
  {
    var builder = new RecurringJobRegistrationBuilder(registry);

    // Runs hourly at minute 0 (first run 00:00)
    builder.For<MatchLifecycleJobService>(s => s.CloseStartingSoonMatches())
      .WithId("close-starting-soon-matches")
      .WithGroup(JobGroups.MatchLifecycle)
      .WithName("Close starting-soon matches")
      .WithDescription("Hourly sweep that closes betting on matches kicking off soon.")
      .WithCron("0 * * * *")
      .Register();

    // Runs once per day at 01:00
    builder.For<PreKickoffDataSyncJobService>(s => s.GetUpcommingSoccerdataMatchesForAllLeagues())
      .WithId("get-soccerdata-upcoming-matches")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Refresh upcoming fixtures")
      .WithDescription("Discovers and syncs scheduled matches across tracked leagues.")
      .Visible()
      .WithCron("0 1 * * *")
      .Register();

    // Runs once per day at 02:00
    builder.For<PreKickoffDataSyncJobService>(s => s.ScheduleRefreshHead2HeadForUpcomingMatches())
      .WithId("refresh-head2head-upcoming-matches")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Queue head-to-head updates")
      .WithDescription("Prepares recent meetings between opponents for upcoming fixtures.")
      .Visible()
      .WithCron("0 2 * * *")
      .Register();

    // Runs once per day at 02:01
    builder.For<PreKickoffDataSyncJobService>(s => s.ScheduleMissingPreviewJobsForUpcomingMatches())
      .WithId("get-missing-match-previews")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Queue match previews")
      .WithDescription("Schedules preview work for upcoming matches that do not yet have one.")
      .Visible()
      .WithCron("1 2 * * *")
      .Register();

    // Runs once per day at 03:00 UTC
    builder.For<BankrollJobService>(s => s.ApplyPaydayIfDue())
      .WithId("apply-payday-if-due")
      .WithGroup(JobGroups.Bankroll)
      .WithName("Apply payday if due")
      .WithDescription("Applies salary when today is payday (last day of month).")
      .WithCron("0 3 * * *")
      .Register();

    // Runs once per day at 04:00
    builder.For<LeagueTableJobService>(s => s.GetLeagueTable())
      .WithId("get-league-table")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Update standings")
      .WithDescription("Refreshes league table positions and points.")
      .Visible()
      .WithCron("0 4 * * *")
      .Register();

    // Runs once per day at 04:15 UTC
    builder.For<MemoryCleanupCronService>(s => s.RunAsync())
      .WithId("betting-agent-memory-cleanup")
      .WithGroup(JobGroups.Maintenance)
      .WithName("Prune stale fixture notes")
      .WithDescription("Removes outdated fixture-specific context so research stays relevant.")
      .Visible()
      .WithCron("15 4 * * *")
      .Register();

    // Runs once per day at 05:00
    builder.For<BookmakerListingSyncJobService>(s => s.GetBetclicGames())
      .WithId("get-upcoming-betclic-games")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Refresh bookmaker fixture links")
      .WithDescription("Aligns listed bookmaker events with the fixture calendar.")
      .Visible()
      .WithCron("0 5 * * *")
      .Register();

    // Runs once per day at 08:00
    builder.For<ClubDailyBriefJobService>(s => s.UpdateClubOverview())
      .WithId("update-clubs-overview")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Refresh club digests")
      .WithDescription("Updates daily club summaries and context.")
      .Visible()
      .WithCron("0 8 * * *")
      .Register();

    // Runs once per day at 09:00
    builder.For<LineupJobService>(s => s.GetLineups())
      .WithId("get-lineups")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Update expected lineups")
      .WithDescription("Refreshes projected starting elevens for upcoming matches.")
      .Visible()
      .WithCron("0 9 * * *")
      .Register();

    // Runs once per day at 11:00
    builder.For<BookmakerListingSyncJobService>(s => s.ScheduleBettingOddsJob())
      .WithId("get-betting-odds")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Queue odds refresh")
      .WithDescription("Enqueues per-fixture updates for current betting odds.")
      .Visible()
      .WithCron("0 11 * * *")
      .Register();

    // Runs once per day at 10:00
    builder.For<UpcomingMatchesInternetResearchCronService>(s => s.RunAsync())
      .WithId("betting-agent-upcoming-internet-research")
      .WithGroup(JobGroups.Research)
      .WithName("Broad fixture research")
      .WithDescription("Portfolio-level news and context before fixture-by-fixture research.")
      .Visible()
      .WithCron("0 10 * * *")
      .Register();

    // Runs once per day at 11:30
    builder.For<BettingAgentCronService>(s => s.RunResearchScheduleAsync())
      .WithId("betting-agent-research")
      .WithGroup(JobGroups.Research)
      .WithName("Daily match research")
      .WithDescription("Runs the scheduled research pass on upcoming fixtures.")
      .Visible()
      .WithCron("30 11 * * *")
      .Register();

    // Runs daily at 13:00
    builder.For<BettingAgentCronService>(s => s.RunBettingExecutionAsync())
      .WithId("betting-agent-execution")
      .WithGroup(JobGroups.Betting)
      .WithName("Execute daily bets")
      .WithDescription("Applies the day's staking and placement decisions.")
      .Visible()
      .WithCron("0 13 * * *")
      .Register();

    // Runs once per day at 22:30
    builder.For<BetslipSettlementJobService>(s => s.ResolveBetslipStatuses())
      .WithId("resolve-betslip-statuses")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Resolve betslip statuses")
      .WithDescription("Settles pending bet selections and updates betslip statuses.")
      .WithCron("30 22 * * *")
      .Register();

    // Runs once per day at 23:00
    builder.For<FinishedMatchScoreJobService>(s => s.FillMissingFinishedMatchScoresFromSoccerData())
      .WithId("fill-missing-finished-match-scores")
      .WithGroup(JobGroups.DataPreparation)
      .WithName("Complete finished match results")
      .WithDescription("Backfills final scores for finished games that are still incomplete.")
      .Visible()
      .WithCron("0 23 * * *")
      .Register();

    // Runs daily at 23:40
    builder.For<BettingAgentCronService>(s => s.RunReflectionAsync())
      .WithId("betting-agent-reflection")
      .WithGroup(JobGroups.Reflection)
      .WithName("End-of-day review")
      .WithDescription("Summarizes outcomes and refreshes strategy notes.")
      .Visible()
      .WithCron("40 23 * * *")
      .Register();
  }

}
