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
      .UseRecommendedSerializerSettings()
      .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

    services.AddHangfireServer(options => options.WorkerCount = 3);

    return services;
  }

  public static void UseRecurringJobs()
  {
    RecurringJob.AddOrUpdate<JobService>(
      "get-soccerdata-upcoming-matches",
      jobService => jobService.GetUpcommingSoccerdataMatches(SoccerDataConstants.PremierLeagueId),
      "0 1 * * *");

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

    // Runs once per day at 10:00
    RecurringJob.AddOrUpdate<JobService>(
        "get-league-table",
        jobService => jobService.GetLeagueTable(),
        "0 10 * * *");

    // Runs hourly at minute 0
    RecurringJob.AddOrUpdate<JobService>(
        "close-starting-soon-matches",
        jobService => jobService.CloseStartingSoonMatches(),
        "0 * * * *");

    // Runs every 6 hours at 15 min past the hour (00:15, 06:15, 12:15, 18:15)
    RecurringJob.AddOrUpdate<JobService>(
        "get-betting-odds",
        jobService => jobService.ScheduleBettingOddsJob(),
        "15 */6 * * *");
  }
}
