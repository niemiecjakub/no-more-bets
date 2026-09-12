using MediatR;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using Microsoft.Extensions.Logging;

namespace NoMoreBets.Application.Clubs.GetClubRecentGames;

public record GetClubRecentGamesQuery(int ClubId, DateOnly? Date = null) : IRequest<IReadOnlyList<RecentMatch>?>;

public sealed class GetClubRecentGamesHandler(IMatchRepository matches, IClubRepository clubs, ILogger<GetClubRecentGamesHandler>? logger = null) : IRequestHandler<GetClubRecentGamesQuery, IReadOnlyList<RecentMatch>?>
{
  public async Task<IReadOnlyList<RecentMatch>?> Handle(GetClubRecentGamesQuery request, CancellationToken cancellationToken)
  {
    var club = await clubs.GetByIdAsync(request.ClubId, cancellationToken).ConfigureAwait(false);
    if (club == null)
    {
      logger?.LogWarning("Club {ClubId} not found while querying recent games.", request.ClubId);
      return null;
    }

    var recentMatchList = await matches.GetRecentMatchesForClubAsync(request.ClubId, 5, request.Date, cancellationToken).ConfigureAwait(false);
    if (recentMatchList.Count == 0)
    {
      logger?.LogWarning("No recent games found for club {ClubId} up to date {Date}.", request.ClubId, request.Date);
      return Array.Empty<RecentMatch>();
    }

    var recentMatches = new List<RecentMatch>(recentMatchList.Count);
    foreach (var m in recentMatchList)
    {
      var isHome = m.HomeClubId == request.ClubId;
      var opponentName = isHome ? m.AwayClub.Name : m.HomeClub.Name;
      var homeGoals = m.HomeGoals ?? 0;
      var awayGoals = m.AwayGoals ?? 0;
      var score = $"{homeGoals} : {awayGoals}";
      var result = isHome
        ? (homeGoals > awayGoals ? "Win" : homeGoals < awayGoals ? "Loss" : "Draw")
        : (awayGoals > homeGoals ? "Win" : awayGoals < homeGoals ? "Loss" : "Draw");
      recentMatches.Add(new RecentMatch(MatchId: m.Id, Opponent: opponentName, Score: score, Result: result, Date: DateOnly.FromDateTime(m.MatchDate)));
    }

    return recentMatches.OrderByDescending(g => g.Date).ToList();
  }
}
