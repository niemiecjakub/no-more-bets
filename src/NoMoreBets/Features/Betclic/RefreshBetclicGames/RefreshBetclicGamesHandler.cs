using MediatR;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Features.Betclic.Scraping;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Betclic.RefreshBetclicGames;

/// <summary>
/// Command to refresh Betclic games: fetches upcoming games from Betclic, and adds any match that does not yet exist (same teams, same day) to the database.
/// </summary>
public record RefreshBetclicGamesCommand : IRequest<IReadOnlyList<Match>>;

/// <summary>
/// Handles <see cref="RefreshBetclicGamesCommand"/>: calls <see cref="BetclicScraper.GetUpcomingGamesAsync"/>,
/// for each game checks if a match for those teams on that day already exists; if not, adds it to the database.
/// </summary>
public class RefreshBetclicGamesHandler(
  BetclicScraper scraper,
  AppDbContext db,
  IMatchMatcher matchMatcher) : IRequestHandler<RefreshBetclicGamesCommand, IReadOnlyList<Match>>
{
  private const int LeagueId = 1;
  private const int StageId = 1;

  /// <inheritdoc />
  public async Task<IReadOnlyList<Match>> Handle(RefreshBetclicGamesCommand request, CancellationToken cancellationToken)
  {
    var upcomingGames = await scraper.GetUpcomingGamesAsync(cancellationToken).ConfigureAwait(false);
    var added = new List<Match>();
    var hasUpdates = false;

    if (upcomingGames.Count == 0)
    {
      return added;
    }

    var clubs = await db.Club
      .Where(c => c.LeagueId == LeagueId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    foreach (var game in upcomingGames)
    {
      var dateWithTime = CombineDateAndTime(game.Date, game.Time);
      var gameDayUtc = DateTime.SpecifyKind(dateWithTime, DateTimeKind.Utc);

      var matchesOnDay = await db.Match
        .Where(m => m.MatchDate.Date == gameDayUtc.Date)
        .Include(m => m.HomeClub)
        .Include(m => m.AwayClub)
        .ToListAsync(cancellationToken)
        .ConfigureAwait(false);

      var candidates = matchesOnDay
        .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
        .ToList();

      var existing = matchMatcher.FindBestMatch(game.HomeTeam, game.AwayTeam, candidates);
      if (existing is not null)
      {
        if (string.IsNullOrEmpty(existing.BetclicUrl))
        {
          existing.BetclicUrl = game.Url;
          hasUpdates = true;
        }
        continue;
      }

      var homeClub = matchMatcher.FindClub(game.HomeTeam, clubs);
      var awayClub = matchMatcher.FindClub(game.AwayTeam, clubs);
      var newMatch = Match.CreateUpcomming(gameDayUtc, StageId, homeClub.Id, awayClub.Id);
      newMatch.BetclicUrl = game.Url;
      db.Match.Add(newMatch);
      added.Add(newMatch);
    }

    if (added.Count > 0 || hasUpdates)
    {
      await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    return added;
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
