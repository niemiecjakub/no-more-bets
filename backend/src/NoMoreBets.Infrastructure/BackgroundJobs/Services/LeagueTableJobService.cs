using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Leagues.UpdateTable;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class LeagueTableJobService(
  IMediator mediator,
  AppDbContext db,
  ILogger<LeagueTableJobService> logger)
{
  /// <summary>
  /// Schedules <see cref="GetLeagueTableForLeague"/> per league with a random delay (0-900s), same stagger pattern as lineup and Betclic jobs.
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task GetLeagueTable()
  {
    var leagues = await db.League
      .Where(l => l.SoccerdataId > 0)
      .Select(l => new { l.Id, l.Name })
      .ToListAsync();
    if (leagues.Count == 0)
    {
      logger.LogWarning(
        "Job {JobName} found no leagues in database. Skipping refresh.",
        nameof(GetLeagueTable));
      return;
    }

    logger.LogInformation(
      "Starting job {JobName} to schedule league table refresh for {LeagueCount} leagues",
      nameof(GetLeagueTable),
      leagues.Count);

    foreach (var league in leagues)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 900));
      BackgroundJob.Schedule<LeagueTableJobService>(
        js => js.GetLeagueTableForLeague(league.Id),
        delay);
      logger.LogInformation(
        "Job {JobName} scheduled league table refresh for league {LeagueId} ({LeagueName}) after {DelaySeconds}s",
        nameof(GetLeagueTable),
        league.Id,
        league.Name,
        delay.TotalSeconds);
    }

    logger.LogInformation(
      "Job {JobName} scheduled {LeagueCount} per-league table refresh jobs (random delay 0-900s each)",
      nameof(GetLeagueTable),
      leagues.Count);
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task GetLeagueTableForLeague(int leagueId)
  {
    var leagueName = await db.League
      .Where(l => l.Id == leagueId)
      .Select(l => l.Name)
      .FirstOrDefaultAsync()
      .ConfigureAwait(false);

    logger.LogInformation(
      "Starting job {JobName} for league {LeagueId} ({LeagueName})",
      nameof(GetLeagueTableForLeague),
      leagueId,
      leagueName ?? "(unknown)");

    await mediator.Send(new UpdateTableCommand(leagueId)).ConfigureAwait(false);

    logger.LogInformation(
      "Completed job {JobName} for league {LeagueId}",
      nameof(GetLeagueTableForLeague),
      leagueId);
  }
}
