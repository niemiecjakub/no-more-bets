using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Betting.UpdateMatches;

/// <summary>
/// Command to refresh Betclic games: fetches upcoming games from Betclic, and adds any match that does not yet exist (same teams, same day) to the database.
/// </summary>
public record UpdateMatchesCommand(int LeagueId) : IRequest<IReadOnlyList<Match>>;

/// <summary>
/// Handles <see cref="UpdateMatchesCommand"/>: calls <see cref="IBookmakerMatchesProvider.GetUpcomingGamesAsync"/>,
/// for each game checks if a match for those teams on that day already exists; if not, adds it to the database.
/// </summary>
public class UpdateMatchesHandler(
  IBookmakerMatchesProvider bookmakerMatchesProvider,
  IUnitOfWork unitOfWork,
  IMatchMatcher matchMatcher) : IRequestHandler<UpdateMatchesCommand, IReadOnlyList<Match>>
{
  /// <inheritdoc />
  public async Task<IReadOnlyList<Match>> Handle(UpdateMatchesCommand request, CancellationToken cancellationToken)
  {
    var league = (await unitOfWork.Leagues.GetLeagues())
      .FirstOrDefault(l => l.Id == request.LeagueId)
      ?? throw new InvalidOperationException($"League with id {request.LeagueId} not found.");

    var upcomingGames = await bookmakerMatchesProvider.GetUpcomingGamesAsync(league.Slug, cancellationToken);
    var added = new List<Match>();
    var hasUpdates = false;

    if (upcomingGames.Count == 0)
    {
      return added;
    }

    var allClubs = await unitOfWork.Clubs.GetClubs(league.Id);
    var stage = await unitOfWork.Leagues.GetCurrentStage(league.SoccerdataId);

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

      var homeClub = matchMatcher.FindClub(game.HomeTeam, allClubs);
      var awayClub = matchMatcher.FindClub(game.AwayTeam, allClubs);
      if (homeClub.LeagueId != awayClub.LeagueId)
      {
        throw new InvalidOperationException(
          $"Matched clubs '{homeClub.Name}' and '{awayClub.Name}' belong to different leagues.");
      }

      var newMatch = Match.CreateUpcomming(gameDayUtc, stage.Id, homeClub.Id, awayClub.Id);
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
