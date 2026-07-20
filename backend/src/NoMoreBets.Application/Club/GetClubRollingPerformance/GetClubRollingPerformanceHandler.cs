using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubRollingPerformance;

public record GetClubRollingPerformanceQuery(int ClubId, DateOnly? Date = null) : IRequest<TeamPerformanceResult?>;

public sealed class GetClubRollingPerformanceHandler(IUnitOfWork unitOfWork, ILogger<GetClubRollingPerformanceHandler>? logger = null) : IRequestHandler<GetClubRollingPerformanceQuery, TeamPerformanceResult?>
{
  public async Task<TeamPerformanceResult?> Handle(GetClubRollingPerformanceQuery request, CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs.GetByIdAsync(request.ClubId, cancellationToken).ConfigureAwait(false);
    if (club == null)
    {
      logger?.LogWarning("Club {ClubId} not found while querying rolling performance.", request.ClubId);
      return null;
    }

    var matches = await unitOfWork.Matches.GetRecentMatchesForClubAsync(request.ClubId, 5, request.Date, cancellationToken).ConfigureAwait(false);
    if (matches.Count == 0)
    {
      logger?.LogWarning("No recent matches found for rolling performance query up to date {Date}. ClubId={ClubId}", request.Date, request.ClubId);
      return new TeamPerformanceResult(
        TopPlayers: Array.Empty<PlayerRecentRatings>(),
        RecentTeamRatings: Array.Empty<double>(),
        AvgTeamRating: 0,
        Formations: Array.Empty<string>());
    }

    var matchesByDateAsc = matches.OrderBy(m => m.MatchDate).ToList();
    var playerToRatingsAndDates = new Dictionary<string, List<(double Rating, DateTime MatchDate)>>(StringComparer.Ordinal);
    var teamRatingsAndDates = new List<(double Rating, DateTime MatchDate)>();
    var formationsByDate = new List<(string Formation, DateTime MatchDate)>();

    foreach (var match in matchesByDateAsc)
    {
      var details = await unitOfWork.Matches.GetMatchDetailsByMatchIdAsync(match.Id, cancellationToken).ConfigureAwait(false);
      var payload = details?.GetFotmobDetails();
      if (payload == null)
        continue;

      var lineup = match.HomeClubId == request.ClubId ? payload.HomeLineup : payload.AwayLineup;
      if (lineup == null)
        continue;

      if (lineup.TeamRating.HasValue)
        teamRatingsAndDates.Add((lineup.TeamRating.Value, match.MatchDate));
      formationsByDate.Add((lineup.Formation ?? string.Empty, match.MatchDate));

      if (lineup.Players == null)
        continue;

      foreach (var p in lineup.Players)
      {
        if (!p.Rating.HasValue)
          continue;
        var name = p.Name;
        if (string.IsNullOrWhiteSpace(name))
          continue;
        if (!playerToRatingsAndDates.TryGetValue(name, out var list))
        {
          list = new List<(double, DateTime)>();
          playerToRatingsAndDates[name] = list;
        }

        list.Add((p.Rating.Value, match.MatchDate));
      }
    }

    var recentTeamRatings = teamRatingsAndDates.OrderBy(x => x.MatchDate).Select(x => x.Rating).ToList();
    var avgTeamRating = recentTeamRatings.Count > 0 ? Math.Round(recentTeamRatings.Average(), 2) : 0d;
    var formations = formationsByDate.OrderBy(x => x.MatchDate).Select(x => x.Formation).ToList();

    var topPlayers = playerToRatingsAndDates
      .Select(kv =>
      {
        var sorted = kv.Value.OrderBy(x => x.MatchDate).ToList();
        var recentRatings = sorted.Select(x => x.Rating).ToList();
        var avgRating = recentRatings.Count > 0 ? recentRatings.Average() : 0d;
        return new PlayerRecentRatings(Player: kv.Key, RecentRatings: recentRatings, AvgRating: Math.Round(avgRating, 2));
      })
      .OrderByDescending(p => p.AvgRating)
      .ToList();

    return new TeamPerformanceResult(
      TopPlayers: topPlayers,
      RecentTeamRatings: recentTeamRatings,
      AvgTeamRating: avgTeamRating,
      Formations: formations);
  }
}
