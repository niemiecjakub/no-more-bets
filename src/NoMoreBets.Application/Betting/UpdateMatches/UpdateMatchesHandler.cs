using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Betting.UpdateMatches;

/// <summary>
/// Command to refresh Betclic games: fetches upcoming games from Betclic, and adds any match that does not yet exist (same teams, same day) to the database.
/// </summary>
public record UpdateMatchesCommand : IRequest<IReadOnlyList<Match>>;

/// <summary>
/// Handles <see cref="UpdateMatchesCommand"/>: calls <see cref="BetclicScraper.GetUpcomingGamesAsync"/>,
/// for each game checks if a match for those teams on that day already exists; if not, adds it to the database.
/// </summary>
public class UpdateMatchesHandler(
  IBookmakerMatchesProvider bookmakerMatchesProvider,
  IUnitOfWork unitOfWork,
  IMatchMatcher matchMatcher) : IRequestHandler<UpdateMatchesCommand, IReadOnlyList<Match>>
{
  private const int LeagueId = 1;
  private const int StageId = 1;

  /// <inheritdoc />
  public async Task<IReadOnlyList<Match>> Handle(UpdateMatchesCommand request, CancellationToken cancellationToken)
  {
    var upcomingGames = await bookmakerMatchesProvider.GetUpcomingGamesAsync(cancellationToken);
    var added = new List<Match>();
    var hasUpdates = false;

    if (upcomingGames.Count == 0)
    {
      return added;
    }

    var clubs = await unitOfWork.Clubs.GetClubs(LeagueId);

    foreach (var game in upcomingGames)
    {
      var dateWithTime = CombineDateAndTime(game.Date, game.Time);
      var gameDayUtc = DateTime.SpecifyKind(dateWithTime, DateTimeKind.Utc);
      var matchesOnDay = await unitOfWork.Matches.GetMatches(gameDayUtc);

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
      await unitOfWork.Matches.AddMatch(newMatch);
      added.Add(newMatch);
    }

    if (added.Count > 0 || hasUpdates)
    {
      await unitOfWork.SaveChangesAsync(cancellationToken);
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
