using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Matches.UpdateLineup;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

public sealed class LineupJobService(
  IMediator mediator,
  AppDbContext db,
  ILineupProvider lineupProvider,
  ILogger<LineupJobService> logger)
{
  /// <summary>
  /// Schedules <see cref="GetLineupsForLeague"/> per RotoWire-supported league with a random delay (0-900s), same stagger pattern as Betclic league jobs.
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task GetLineups()
  {
    logger.LogInformation(
      "Starting job {JobName} to schedule per-league RotoWire lineup refreshes",
      nameof(GetLineups));

    var supported = lineupProvider.SupportedLeagueSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var leagues = (await db.League
        .Select(l => new { l.Id, l.Name, l.Slug })
        .ToListAsync()
        .ConfigureAwait(false))
      .Where(l => supported.Contains(l.Slug))
      .ToList();

    if (leagues.Count == 0)
    {
      logger.LogWarning(
        "Job {JobName} found no leagues in the database with a RotoWire-supported slug. Skipping.",
        nameof(GetLineups));
      return;
    }

    logger.LogInformation(
      "Job {JobName} scheduling lineup refresh for {LeagueCount} leagues (RotoWire-supported slugs only)",
      nameof(GetLineups),
      leagues.Count);

    foreach (var league in leagues)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 900));
      BackgroundJob.Schedule<LineupJobService>(
        js => js.GetLineupsForLeague(league.Id),
        delay);
      logger.LogInformation(
        "Job {JobName} scheduled RotoWire lineup refresh for league {LeagueId} ({LeagueName}) after {DelaySeconds}s",
        nameof(GetLineups),
        league.Id,
        league.Name,
        delay.TotalSeconds);
    }

    logger.LogInformation(
      "Job {JobName} scheduled {LeagueCount} per-league RotoWire lineup jobs",
      nameof(GetLineups),
      leagues.Count);
  }

  [AutomaticRetry(Attempts = 3)]
  public async Task GetLineupsForLeague(int leagueId)
  {
    logger.LogInformation(
      "Starting job {JobName} for league {LeagueId}",
      nameof(GetLineupsForLeague),
      leagueId);

    await mediator.Send(new UpdateLineupsCommand(leagueId)).ConfigureAwait(false);

    logger.LogInformation(
      "Completed job {JobName} for league {LeagueId}",
      nameof(GetLineupsForLeague),
      leagueId);
  }
}
