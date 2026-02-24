using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Infrastructure.Database;
using DomainMatch = NoMoreBets.Domain.Entity.Match;
namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

public class RefreshSoccerDataMatchPreviewsUpcomingHandler(
  SoccerDataClient client,
  AppDbContext db,
  IMatchMatcher matchMatcher,
  ILogger<RefreshSoccerDataMatchPreviewsUpcomingHandler> logger)
  : IRequestHandler<RefreshSoccerDataMatchPreviewsUpcomingCommand, List<DomainMatch>>
{
  public async Task<List<DomainMatch>> Handle(RefreshSoccerDataMatchPreviewsUpcomingCommand request, CancellationToken cancellationToken)
  {
    var added = new List<DomainMatch>();
    var previews = await client.GetMatchPreviewsUpcomingAsync(request.SoccerdataLeagueId, cancellationToken)
      .ConfigureAwait(false);

    var clubIds = previews
      .SelectMany(l => l.MatchPreviews)
      .SelectMany(p => new[] { p.Teams.Home.Id, p.Teams.Away.Id })
      .Distinct()
      .ToList();

    var clubsBySoccerdataId = await db.Club
      .Where(c => clubIds.Contains(c.SoccerdataId))
      .ToDictionaryAsync(c => c.SoccerdataId, cancellationToken)
      .ConfigureAwait(false);

    var leagues = await db.League.Select(c => c.SoccerdataId).ToListAsync();
    foreach (var league in previews)
    {
      if (!leagues.Contains(league.LeagueId))
      {
        continue;
      }

      var currentStageId = await db.Stage
          .Where(s => s.Season.League.SoccerdataId == league.LeagueId)
          .OrderByDescending(s => s.Id)
          .Select(s => s.Id)
          .FirstOrDefaultAsync();

      foreach (var matchPreview in league.MatchPreviews)
      {
        if (!TryParseMatchDate(matchPreview.Date, matchPreview.Time, out var gameDayUtc))
        {
          continue;
        }

        var matchesOnDay = await db.Match
          .Where(m => m.MatchDate.Date == gameDayUtc.Date)
          .Include(m => m.HomeClub)
          .Include(m => m.AwayClub)
          .ToListAsync(cancellationToken)
          .ConfigureAwait(false);

        var candidates = matchesOnDay
          .Select(m => (m.HomeClub.Name, m.AwayClub.Name, (DomainMatch)m))
          .ToList();

        var matched = matchMatcher.FindBestMatch(matchPreview.Teams.Home.Name, matchPreview.Teams.Away.Name, candidates);

        if (matched is not null)
        {
          if (matched.SoccerdataId is null)
          {
            matched.SoccerdataId = matchPreview.Id;
          }
          continue;
        }

        if (!clubsBySoccerdataId.TryGetValue(matchPreview.Teams.Home.Id, out var homeClub) ||
            !clubsBySoccerdataId.TryGetValue(matchPreview.Teams.Away.Id, out var awayClub))
        {
          logger.LogWarning(
            "Skipping insert for match {MatchId} ({Home} vs {Away}): missing club(s) in DB. HomeClubId={HomeSoccerdataId}, AwayClubId={AwaySoccerdataId}",
            matchPreview.Id,
            matchPreview.Teams.Home.Name,
            matchPreview.Teams.Away.Name,
            matchPreview.Teams.Home.Id,
            matchPreview.Teams.Away.Id);
          continue;
        }

        var newMatch = DomainMatch.CreateUpcomming(gameDayUtc, currentStageId, homeClub.Id, awayClub.Id);
        newMatch.SoccerdataId = matchPreview.Id;
        db.Match.Add(newMatch);
        added.Add(newMatch);
      }
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return added;
  }

  private static bool TryParseMatchDate(string dateStr, string timeStr, out DateTime gameDayUtc)
  {
    gameDayUtc = default;
    if (string.IsNullOrWhiteSpace(dateStr))
    {
      return false;
    }

    if (!DateTime.TryParseExact(
      $"{dateStr.Trim()} {timeStr?.Trim() ?? "00:00"}",
      new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy H:mm", "dd/MM/yyyy" },
      CultureInfo.InvariantCulture,
      DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
      out var parsed))
    {
      return false;
    }

    gameDayUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    return true;
  }
}
