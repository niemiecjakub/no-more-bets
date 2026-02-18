using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;
using NoMoreBets.Features.Betclic.Scraping;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;

/// <summary>
/// Handles <see cref="GetBetclicUpcomingGamesQuery"/> by delegating to <see cref="IBetclicScraper"/> and resolving clubs from DB.
/// </summary>
public class GetBetclicUpcomingGamesHandler(
  IBetclicScraper scraper,
  AppDbContext db,
  IMatchMatcher matchMatcher) : IRequestHandler<GetBetclicUpcomingGamesQuery, IReadOnlyList<UpcomingGameDto>>
{
  /// <inheritdoc />
  public async Task<IReadOnlyList<UpcomingGameDto>> Handle(GetBetclicUpcomingGamesQuery request, CancellationToken cancellationToken)
  {
    var upcomingGames = await scraper.GetUpcomingGamesAsync(cancellationToken).ConfigureAwait(false);
    if (upcomingGames.Count == 0)
    {
      return Array.Empty<UpcomingGameDto>();
    }

    var results = new List<UpcomingGameDto>(upcomingGames.Count);
    foreach (var game in upcomingGames)
    {
      var dateWithTime = CombineDateAndTime(game.Date, game.Time);
      var gameDayUtc = DateTime.SpecifyKind(dateWithTime, DateTimeKind.Utc);

      var matchesOnDay = await db.Match
        .Where(g => g.MatchDate.Date == gameDayUtc)
        .Include(g => g.HomeClub)
        .Include(g => g.AwayClub)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

      var candidates = matchesOnDay
        .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
        .ToList();

      Match? matched = matchMatcher.FindBestMatch(game.HomeTeam, game.AwayTeam, candidates);

      if (matched is null)
      {
        const int leagueId = 1;
        const int stageId = 1;
        var clubs = await db.Club
          .Where(c => c.LeagueId == leagueId)
          .ToListAsync(cancellationToken)
          .ConfigureAwait(false);
        var homeClub = matchMatcher.FindClub(game.HomeTeam, clubs);
        var awayClub = matchMatcher.FindClub(game.AwayTeam, clubs);
        var newMatch = Match.CreateUpcomming(gameDayUtc, stageId, homeClub.Id, awayClub.Id);
        db.Match.Add(newMatch);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        newMatch.HomeClub = homeClub;
        newMatch.AwayClub = awayClub;
        matched = newMatch;
      }

      var m = matched!;
      var homeTeam = new UpcomingGameTeamDto(m.HomeClub.Id, m.HomeClub.Name, m.HomeClub.SoccerdataId);
      var awayTeam = new UpcomingGameTeamDto(m.AwayClub.Id, m.AwayClub.Name, m.AwayClub.SoccerdataId);
      results.Add(new UpcomingGameDto(m.Id, m.SoccerdataId, dateWithTime, homeTeam, awayTeam, game.Url));
    }

    return results;
  }

  private static DateTime CombineDateAndTime(DateTime date, string time)
  {
    if (string.IsNullOrWhiteSpace(time))
    {
      return date;
    }
    if (TimeOnly.TryParse(time.Trim(), out var t))
    {
      return date.Date.Add(t.ToTimeSpan());
    }
    return date;
  }
}
