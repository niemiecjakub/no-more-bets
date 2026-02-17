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
      var gameDayUtc = dateWithTime.ToUniversalTime().Date;

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

      var homeTeam = matched != null
        ? new UpcomingGameTeamDto(matched.HomeClub.Id, matched.HomeClub.Name, matched.HomeClub.SoccerdataId)
        : new UpcomingGameTeamDto(0, game.HomeTeam, 0);
      var awayTeam = matched != null
        ? new UpcomingGameTeamDto(matched.AwayClub.Id, matched.AwayClub.Name, matched.AwayClub.SoccerdataId)
        : new UpcomingGameTeamDto(0, game.AwayTeam, 0);

      var gameId = matched?.Id ?? 0;
      var gameSoccerdataId = matched?.SoccerdataId;
      results.Add(new UpcomingGameDto(gameId, gameSoccerdataId, dateWithTime, homeTeam, awayTeam, game.Url));
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
