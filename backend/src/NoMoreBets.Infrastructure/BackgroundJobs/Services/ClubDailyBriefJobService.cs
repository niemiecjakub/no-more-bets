using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Clubs.GetOverview;
using NoMoreBets.Application.Clubs.UpdateDailySummary;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Matches.UpdateMatchDetails;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;

namespace NoMoreBets.Infrastructure.BackgroundJobs;

/// <summary>
/// Refreshes per-club daily narratives and persists recent match detail pages used in club context.
/// </summary>
public sealed class ClubDailyBriefJobService(
  IMediator mediator,
  AppDbContext db,
  IFotmobConstants fotmobConstants,
  ILogger<ClubDailyBriefJobService> logger)
{
  /// <summary>
  /// Enqueues daily summary updates for clubs with an upcoming match within 10 days.
  /// Uses staggered delays so club summary and match-detail jobs do not collide.
  /// </summary>
  [AutomaticRetry(Attempts = 1)]
  public async Task UpdateClubOverview()
  {
    logger.LogInformation(
      "Starting job {JobName} to enqueue daily summary updates for clubs with upcoming matches within 10 days",
      nameof(UpdateClubOverview));

    var utcNow = DateTime.UtcNow;
    var kickoffWithinTenDaysEnd = utcNow.AddDays(10);

    var clubs = await db.Club
      .Where(c => db.Match.Any(m =>
        m.MatchStatusId == (int)MatchStatus.Upcomming
        && m.MatchDate > utcNow
        && m.MatchDate <= kickoffWithinTenDaysEnd
        && (m.HomeClubId == c.Id || m.AwayClubId == c.Id)))
      .Select(c => new { c.Id, c.Name })
      .ToListAsync();

    logger.LogInformation(
      "Job {JobName} found {ClubCount} clubs to update",
      nameof(UpdateClubOverview),
      clubs.Count);

    var fotmobMatchUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var club in clubs)
    {
      var fotmobTeam = fotmobConstants.GetTeamByName(club.Name);
      if (fotmobTeam == null)
      {
        logger.LogDebug(
          "Job {JobName} skipping club {ClubId} ({ClubName}): no Fotmob team mapping",
          nameof(UpdateClubOverview),
          club.Id,
          club.Name);
        continue;
      }

      ClubOverview clubOverview;
      try
      {
        clubOverview = await mediator.Send(new GetClubOverviewQuery(fotmobTeam.Id)).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        logger.LogWarning(
          ex,
          "Job {JobName} failed to get overview for club {ClubId} ({ClubName}), FotmobTeamId={FotmobTeamId}",
          nameof(UpdateClubOverview),
          club.Id,
          club.Name,
          fotmobTeam.Id);
        continue;
      }

      if (!string.IsNullOrWhiteSpace(clubOverview.DailySummary))
      {
        var delay = TimeSpan.FromSeconds(Random.Shared.Next(0, 300));
        BackgroundJob.Schedule<ClubDailyBriefJobService>(js => js.UpdateDailySummaryForClub(club.Id, clubOverview.DailySummary), delay);
      }

      foreach (var recentGame in clubOverview.RecentGames)
      {
        fotmobMatchUrls.Add(recentGame.GameUrl);
      }
    }

    foreach (var url in fotmobMatchUrls)
    {
      var delay = TimeSpan.FromSeconds(Random.Shared.Next(500, 2000));
      BackgroundJob.Schedule<ClubDailyBriefJobService>(js => js.UpdateMatchDetails(url), delay);
    }
  }

  public async Task UpdateDailySummaryForClub(int clubId, string summary)
  {
    await mediator.Send(new UpdateDailySummaryCommand(clubId, summary));
  }

  public async Task UpdateMatchDetails(string fotmobMatchUrl)
  {
    var result = await mediator.Send(new UpdateMatchDetailsCommand(fotmobMatchUrl));
    if (result.CreatedNewMatch)
    {
      logger.LogInformation(
        "Job {JobName} created new match {MatchId} while syncing Fotmob recent game details for URL {FotmobMatchUrl}",
        nameof(UpdateMatchDetails),
        result.MatchId,
        fotmobMatchUrl);
    }
  }
}
