using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Infrastructure.BackgroundJobs;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;

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
    // Runs once per day at 00:05 (5 minutes after midnight)
    RecurringJob.AddOrUpdate<JobService>(
      "get-soccerdata-upcoming-matches",
      jobService => jobService.GetUpcommingSoccerdataMatches(SoccerDataConstants.PremierLeagueId),
      "5 0 * * *");

    // Runs once per day at 00:15 – refresh previews and head-to-head for upcoming matches
    RecurringJob.AddOrUpdate<JobService>(
      "refresh-upcoming-match-previews-and-head2head",
      jobService => jobService.RefreshUpcomingMatchPreviewsAndHead2Head(),
      "15 0 * * *");

    // Runs once per day at 15:00
    RecurringJob.AddOrUpdate<JobService>(
        "get-upcoming-betclic-games",
        jobService => jobService.GetBetclicGames(),
        "0 15 * * *");

    // Runs once per day at 16:00
    RecurringJob.AddOrUpdate<JobService>(
        "get-lineups",
        jobService => jobService.GetLineups(),
        "0 16 * * *");

    // Runs once per day at 18:00
    RecurringJob.AddOrUpdate<JobService>(
        "generate-match-predictions",
        jobService => jobService.GenerateMissingMatchPredictions(),
        "0 18 * * *");

    // Runs once per day at 10:00
    RecurringJob.AddOrUpdate<JobService>(
        "get-league-table",
        jobService => jobService.GetLeagueTable(),
        "0 10 * * *");

    // Runs once per day at 14:00
    RecurringJob.AddOrUpdate<JobService>(
        "update-clubs-overview",
        jobService => jobService.UpdateClubOverview(),
        "0 14 * * *");

    // Runs hourly at minute 0
    RecurringJob.AddOrUpdate<JobService>(
        "close-starting-soon-matches",
        jobService => jobService.CloseStartingSoonMatches(),
        "0 * * * *");

    // Runs once per day at 17:00
    RecurringJob.AddOrUpdate<JobService>(
        "get-betting-odds",
        jobService => jobService.ScheduleBettingOddsJob(),
        "0 17 * * *");

    // Runs once per day at 23:50
    RecurringJob.AddOrUpdate<JobService>(
        "fill-missing-finished-match-scores",
        jobService => jobService.FillMissingFinishedMatchScoresFromSoccerData(),
        "50 23 * * *");

    // Runs once per day at 00:20 UTC — applies salary when today is payday (last day of month)
    RecurringJob.AddOrUpdate<JobService>(
        "apply-payday-if-due",
        jobService => jobService.ApplyPaydayIfDue(),
        "20 0 * * *");

    // Runs every 6 hours at minute 0 (00:00, 06:00, 12:00, 18:00)
    RecurringJob.AddOrUpdate<ResearchCronService>(
        "betting-agent-research",
        s => s.RunAsync(),
        "0 */6 * * *");

    // Runs daily at 18:30
    RecurringJob.AddOrUpdate<BettingCronService>(
        "betting-agent-execution",
        s => s.RunAsync(),
        "30 18 * * *");

    // Runs daily at 02:30
    RecurringJob.AddOrUpdate<ReflectionCronService>(
        "betting-agent-reflection",
        s => s.RunAsync(),
        "30 2 * * *");
  }
}
