using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Clubs.GetOverview;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Fotmob;
using NoMoreBets.Application.Matches.UpdateMatchDetails;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Matches.BackfillRecentFotmobMatchDetails;

/// <summary>TEMP: Walks Ekstraklasa + LaLiga + Bundesliga + Serie A + Ligue 1 clubs, collects FotMob recent game URLs, runs UpdateMatchDetails per URL.</summary>
public sealed class BackfillRecentFotmobMatchDetailsHandler(
  IUnitOfWork unitOfWork,
  IMediator mediator,
  IFotmobTeamLookup fotmobTeamLookup,
  IOptions<BackfillFotmobMatchDetailsOptions> options,
  ILogger<BackfillRecentFotmobMatchDetailsHandler> logger)
  : IRequestHandler<BackfillRecentFotmobMatchDetailsCommand, BackfillRecentFotmobMatchDetailsResult>
{
  private static readonly HashSet<string> TargetLeagueSlugs = new(StringComparer.OrdinalIgnoreCase)
  {
    "ekstraklasa",
    "laliga",
    "bundesliga",
    "serie-a",
    "ligue-1"
  };

  public async Task<BackfillRecentFotmobMatchDetailsResult> Handle(
    BackfillRecentFotmobMatchDetailsCommand request,
    CancellationToken cancellationToken)
  {
    var delayMs = Math.Max(0, options.Value.DelayBetweenFotmobRequestsMs);

    var leagues = await unitOfWork.Leagues.GetLeagues().ConfigureAwait(false);
    var leagueIds = leagues
      .Where(l => TargetLeagueSlugs.Contains(l.Slug))
      .Select(l => l.Id)
      .Distinct()
      .ToList();

    if (leagueIds.Count == 0)
    {
      logger.LogWarning(
        "TEMP backfill: no leagues matched slugs {Slugs}. Check League rows.",
        string.Join(", ", TargetLeagueSlugs));
      return new BackfillRecentFotmobMatchDetailsResult(0, 0, 0, 0, 0, 0);
    }

    var seenClubIds = new HashSet<int>();
    var clubs = new List<Club>();
    foreach (var leagueId in leagueIds)
    {
      var batch = await unitOfWork.Clubs.GetClubs(leagueId).ConfigureAwait(false);
      foreach (var c in batch)
      {
        if (seenClubIds.Add(c.Id))
          clubs.Add(c);
      }
    }

    clubs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

    var uniqueUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var skippedUnmapped = 0;
    var overviewOk = 0;
    var overviewFailed = 0;

    foreach (var club in clubs)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var fotmobTeamId = fotmobTeamLookup.TryResolveFotmobTeamId(club.Name);
      if (!fotmobTeamId.HasValue)
      {
        skippedUnmapped++;
        logger.LogDebug("TEMP backfill: skip club {ClubId} {Name}: no FotMob name mapping", club.Id, club.Name);
        continue;
      }

      try
      {
        var overview = await mediator
          .Send(new GetClubOverviewQuery(fotmobTeamId.Value), cancellationToken)
          .ConfigureAwait(false);
        overviewOk++;
        foreach (var g in overview.RecentGames)
        {
          var url = g.GameUrl.Trim();
          if (string.IsNullOrWhiteSpace(url))
            continue;
          uniqueUrls.Add(url);
        }
      }
      catch (Exception ex)
      {
        overviewFailed++;
        logger.LogWarning(ex, "TEMP backfill: GetClubOverview failed for club {ClubId} {Name} FotMobTeamId={FotMobId}", club.Id, club.Name, fotmobTeamId.Value);
      }

      if (delayMs > 0)
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    var detailsOk = 0;
    var detailsFailed = 0;
    foreach (var url in uniqueUrls.OrderBy(u => u, StringComparer.OrdinalIgnoreCase))
    {
      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        await mediator.Send(new UpdateMatchDetailsCommand(url), cancellationToken).ConfigureAwait(false);
        detailsOk++;
      }
      catch (Exception ex)
      {
        detailsFailed++;
        logger.LogWarning(ex, "TEMP backfill: UpdateMatchDetails failed for URL {Url}", url);
      }

      if (delayMs > 0)
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    var result = new BackfillRecentFotmobMatchDetailsResult(
      skippedUnmapped,
      overviewOk,
      overviewFailed,
      uniqueUrls.Count,
      detailsOk,
      detailsFailed);

    logger.LogInformation(
      "TEMP backfill finished: SkippedUnmapped={Skipped}, OverviewOk={Ok}, OverviewFailed={Fail}, UniqueUrls={Urls}, DetailsOk={Dok}, DetailsFailed={Dfail}",
      result.ClubsSkippedUnmappedFotmob,
      result.ClubsOverviewSucceeded,
      result.ClubsOverviewFailed,
      result.UniqueFotmobGameUrls,
      result.UpdateMatchDetailsSucceeded,
      result.UpdateMatchDetailsFailed);

    return result;
  }
}
