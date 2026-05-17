using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubRecentGames;

public record GetClubRecentGamesQuery(int ClubId, DateOnly? Date = null) : IRequest<IReadOnlyList<RecentMatch>?>;

public sealed class GetClubRecentGamesHandler(IUnitOfWork unitOfWork, ILogger<GetClubRecentGamesHandler>? logger = null) : IRequestHandler<GetClubRecentGamesQuery, IReadOnlyList<RecentMatch>?>
{
  public async Task<IReadOnlyList<RecentMatch>?> Handle(GetClubRecentGamesQuery request, CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs.GetByIdAsync(request.ClubId, cancellationToken).ConfigureAwait(false);
    if (club == null)
    {
      logger?.LogWarning("Club {ClubId} not found while querying recent games.", request.ClubId);
      return null;
    }

    var matches = await unitOfWork.Matches.GetRecentMatchesForClubAsync(request.ClubId, 5, request.Date, cancellationToken).ConfigureAwait(false);
    if (matches.Count == 0)
    {
      logger?.LogWarning("No recent games found for club {ClubId} up to date {Date}.", request.ClubId, request.Date);
      return Array.Empty<RecentMatch>();
    }

    var recentMatches = new List<RecentMatch>(matches.Count);
    foreach (var m in matches)
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
