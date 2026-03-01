using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.SoccerData.Model;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreviewsUpcoming;

/// <summary>Query to fetch upcoming match previews (from API and persisted to DB). Returns empty list if none.</summary>
public record GetSoccerDataMatchPreviewsUpcomingQuery(int? LeagueId = null) : IRequest<IReadOnlyList<LeagueMatchPreviews>>;

public class GetSoccerDataMatchPreviewsUpcomingHandler(SoccerDataClient client, AppDbContext db, IMatchMatcher matchMatcher, ILogger<GetSoccerDataMatchPreviewsUpcomingHandler> logger) : IRequestHandler<GetSoccerDataMatchPreviewsUpcomingQuery, IReadOnlyList<LeagueMatchPreviews>>
{
  public async Task<IReadOnlyList<LeagueMatchPreviews>> Handle(GetSoccerDataMatchPreviewsUpcomingQuery request, CancellationToken cancellationToken)
  {
    var games = await client.GetMatchPreviewsUpcomingAsync(request.LeagueId, cancellationToken)
        .ConfigureAwait(false);

    var clubIds = games
        .SelectMany(l => l.MatchPreviews)
        .SelectMany(p => new[] { p.Teams.Home.Id, p.Teams.Away.Id })
        .Distinct()
        .ToList();

    var clubsBySoccerdataId = await db.Club
        .Where(c => clubIds.Contains(c.SoccerdataId))
        .ToDictionaryAsync(c => c.SoccerdataId, cancellationToken)
        .ConfigureAwait(false);

    foreach (var league in games)
    {
      if (league.LeagueId != 228)
      {
        continue;
      }

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

        var stageId = 1;
        var newMatch = DomainMatch.CreateUpcomming(gameDayUtc, stageId, homeClub.Id, awayClub.Id);
        newMatch.SoccerdataId = matchPreview.Id;
        db.Match.Add(newMatch);
      }
    }

    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    return games;
  }


  /// <summary>Parses SoccerData date and time strings (e.g. dd/MM/yyyy and HH:mm) into UTC.</summary>
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
