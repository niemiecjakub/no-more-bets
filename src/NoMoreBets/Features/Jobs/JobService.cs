using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Features.Rotowire.GetRotowireLineups;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Jobs;

public class JobService(IMediator mediator, AppDbContext db)
{
  [AutomaticRetry(Attempts = 0)]
  public async Task GetUpcommingSoccerdataMatches(int soccerdataLeagueId, CancellationToken cancellationToken = default)
  {
    var upcommingMatches = await mediator.Send(new RefreshSoccerDataMatchPreviewsUpcomingCommand(soccerdataLeagueId), cancellationToken);
    foreach (var match in upcommingMatches)
    {
      if (!match.SoccerdataId.HasValue)
      {
        continue;
      }
      BackgroundJob.Enqueue(() => GetUpcommingSoccerdataMatchePreview(match.SoccerdataId.Value, cancellationToken));
      BackgroundJob.Enqueue(() => RefreshHead2HeadStatistics(match.HomeClubId, match.AwayClubId, cancellationToken));
    }
  }

  [AutomaticRetry(Attempts = 0)]
  public async Task GetUpcommingSoccerdataMatchePreview(int soccerdataMatchId, CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshSoccerDataMatchPreviewCommand(soccerdataMatchId), cancellationToken);
  }

  [AutomaticRetry(Attempts = 0)]
  public async Task RefreshHead2HeadStatistics(int homeClubId, int awayClubId, CancellationToken cancellationToken = default)
  {
    var clubSoccerdataIds = await db.Club
      .Where(c => c.Id == homeClubId || c.Id == awayClubId)
      .Select(c => new { c.Id, c.SoccerdataId })
      .ToListAsync(cancellationToken);

    if (clubSoccerdataIds.Count != 2)
    {
      return;
    }

    var homeClub = clubSoccerdataIds.FirstOrDefault(c => c.Id == homeClubId);
    var awayClub = clubSoccerdataIds.FirstOrDefault(c => c.Id == awayClubId);
    if (homeClub?.SoccerdataId is not { } homeSoccerdataId || awayClub?.SoccerdataId is not { } awaySoccerdataId)
    {
      return;
    }

    var (homeClubSoccerdataId, awayClubSoccerdataId) = (homeSoccerdataId, awaySoccerdataId);
    var h2h = await db.Head2Head
        .ForClubs(homeClubSoccerdataId, awayClubSoccerdataId)
        .FirstOrDefaultAsync(cancellationToken);

    bool shouldUpdate = h2h == null;

    if (h2h != null)
    {
      var lastFinishedGameDate = await db.Match
          .ForClubs(homeClubSoccerdataId, awayClubSoccerdataId)
          .Where(m => m.MatchStatus == Domain.Enums.MatchStatus.Finished)
          .OrderByDescending(m => m.MatchDate)
          .Select(m => (DateTime?)m.MatchDate)
          .FirstOrDefaultAsync(cancellationToken);

      shouldUpdate = lastFinishedGameDate > h2h.UpdatedAt;
    }

    if (shouldUpdate)
    {
      BackgroundJob.Enqueue(() => mediator.Send(new RefreshSoccerDataHeadToHeadCommand(homeClubSoccerdataId, awayClubSoccerdataId), CancellationToken.None));
    }
  }

  [AutomaticRetry(Attempts = 10)]
  public async Task GetLineups(CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshRotowireLineupsCommand(), cancellationToken);
  }
}
