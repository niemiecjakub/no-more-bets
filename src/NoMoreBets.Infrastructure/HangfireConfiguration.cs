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

    services.AddSingleton<IRecurringJobRegistry, RecurringJobRegistry>();

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
    var registry = app.ApplicationServices.GetRequiredService<IRecurringJobRegistry>();
    RegisterRecurringJobs(registry);
    return app;
  }

  private static void RegisterRecurringJobs(IRecurringJobRegistry registry)
  {
    var builder = new RecurringJobRegistrationBuilder(registry);

    // Runs hourly at minute 0 (first run 00:00)
    builder.For<MatchLifecycleJobService>(s => s.CloseStartingSoonMatches())
      .WithId("close-starting-soon-matches")
      .WithGroup(JobGroups.MatchLifecycle)
      .WithName("Close starting-soon matches")
      .WithDescription("Hourly sweep that closes betting on matches kicking off soon.")
      .Visible()
      .WithCron("0 * * * *")
      .Register();

    // Runs once per day at 01:00
    builder.For<PreKickoffDataSyncJobService>(s => s.GetUpcommingSoccerdataMatchesForAllLeagues())
      .WithId("get-soccerdata-upcoming-matches")
      .WithGroup(JobGroups.PreKickoffSync)
      .WithName("Sync upcoming matches from Soccerdata")
      .WithDescription("Fetches upcoming matches for all leagues from Soccerdata.")
      .Visible()
      .WithCron("0 1 * * *")
      .Register();

    // Runs once per day at 02:00
    builder.For<PreKickoffDataSyncJobService>(s => s.ScheduleRefreshHead2HeadForUpcomingMatches())
      .WithId("refresh-head2head-upcoming-matches")
      .WithGroup(JobGroups.PreKickoffSync)
      .WithName("Schedule head-to-head refresh")
      .WithDescription("Schedules head-to-head refresh jobs for upcoming matches.")
      .Visible()
      .WithCron("0 2 * * *")
      .Register();

    // Runs once per day at 02:01
    builder.For<PreKickoffDataSyncJobService>(s => s.ScheduleMissingPreviewJobsForUpcomingMatches())
      .WithId("get-missing-match-previews")
      .WithGroup(JobGroups.PreKickoffSync)
      .WithName("Schedule missing preview fetches")
      .WithDescription("Schedules preview fetch jobs for upcoming matches missing a preview.")
      .Visible()
      .WithCron("1 2 * * *")
      .Register();

    // Runs once per day at 03:00 UTC
    builder.For<BankrollJobService>(s => s.ApplyPaydayIfDue())
      .WithId("apply-payday-if-due")
      .WithGroup(JobGroups.Bankroll)
      .WithName("Apply payday if due")
      .WithDescription("Applies salary when today is payday (last day of month).")
      .Visible()
      .WithCron("0 3 * * *")
      .Register();

    // Runs once per day at 04:00
    builder.For<LeagueTableJobService>(s => s.GetLeagueTable())
      .WithId("get-league-table")
      .WithGroup(JobGroups.LeagueData)
      .WithName("Refresh league tables")
      .WithDescription("Updates league table data.")
      .Visible()
      .WithCron("0 4 * * *")
      .Register();

    // Runs once per day at 04:15 UTC
    builder.For<MemoryCleanupCronService>(s => s.RunAsync())
      .WithId("betting-agent-memory-cleanup")
      .WithGroup(JobGroups.BettingAgent)
      .WithName("Betting agent memory cleanup")
      .WithDescription("Prunes stale fixture-specific memories for the agent.")
      .Visible()
      .WithCron("15 4 * * *")
      .Register();

    // Runs once per day at 05:00
    builder.For<BookmakerListingSyncJobService>(s => s.GetBetclicGames())
      .WithId("get-upcoming-betclic-games")
      .WithGroup(JobGroups.BookmakerSync)
      .WithName("Sync upcoming Betclic games")
      .WithDescription("Fetches upcoming games from Betclic.")
      .Visible()
      .WithCron("0 5 * * *")
      .Register();

    // Runs once per day at 08:00
    builder.For<ClubDailyBriefJobService>(s => s.UpdateClubOverview())
      .WithId("update-clubs-overview")
      .WithGroup(JobGroups.ClubInsights)
      .WithName("Update clubs overview")
      .WithDescription("Refreshes daily club overview content.")
      .Visible()
      .WithCron("0 8 * * *")
      .Register();

    // Runs once per day at 09:00
    builder.For<LineupJobService>(s => s.GetLineups())
      .WithId("get-lineups")
      .WithGroup(JobGroups.Lineups)
      .WithName("Fetch lineups")
      .WithDescription("Pulls lineup data for upcoming fixtures.")
      .Visible()
      .WithCron("0 9 * * *")
      .Register();

    // Runs once per day at 11:00
    builder.For<BookmakerListingSyncJobService>(s => s.ScheduleBettingOddsJob())
      .WithId("get-betting-odds")
      .WithGroup(JobGroups.BookmakerSync)
      .WithName("Schedule betting odds jobs")
      .WithDescription("Schedules per-fixture betting odds refresh jobs.")
      .Visible()
      .WithCron("0 11 * * *")
      .Register();

    // Runs once per day at 10:00
    builder.For<UpcomingMatchesInternetResearchCronService>(s => s.RunAsync())
      .WithId("betting-agent-upcoming-internet-research")
      .WithGroup(JobGroups.BettingAgent)
      .WithName("Portfolio internet research")
      .WithDescription("Internet research for upcoming fixtures before per-match research.")
      .Visible()
      .WithCron("0 10 * * *")
      .Register();

    // Runs once per day at 11:30
    builder.For<BettingAgentCronService>(s => s.RunResearchScheduleAsync())
      .WithId("betting-agent-research")
      .WithGroup(JobGroups.BettingAgent)
      .WithName("Betting agent research")
      .WithDescription("Runs the betting agent research schedule.")
      .Visible()
      .WithCron("30 11 * * *")
      .Register();

    // Runs daily at 13:00
    builder.For<BettingAgentCronService>(s => s.RunBettingExecutionAsync())
      .WithId("betting-agent-execution")
      .WithGroup(JobGroups.BettingAgent)
      .WithName("Betting agent execution")
      .WithDescription("Executes betting decisions for the agent.")
      .Visible()
      .WithCron("0 13 * * *")
      .Register();

    // Runs once per day at 23:00
    builder.For<FinishedMatchScoreJobService>(s => s.FillMissingFinishedMatchScoresFromSoccerData())
      .WithId("fill-missing-finished-match-scores")
      .WithGroup(JobGroups.Results)
      .WithName("Fill missing finished match scores")
      .WithDescription("Backfills finished match scores from Soccerdata where missing.")
      .Visible()
      .WithCron("0 23 * * *")
      .Register();

    // Runs daily at 23:40
    builder.For<BettingAgentCronService>(s => s.RunReflectionAsync())
      .WithId("betting-agent-reflection")
      .WithGroup(JobGroups.BettingAgent)
      .WithName("Betting agent reflection")
      .WithDescription("Runs end-of-day reflection for the betting agent.")
      .Visible()
      .WithCron("40 23 * * *")
      .Register();
  }

}
