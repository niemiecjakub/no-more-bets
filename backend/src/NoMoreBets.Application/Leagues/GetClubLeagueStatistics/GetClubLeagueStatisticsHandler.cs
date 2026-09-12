using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Leagues.GetClubLeagueStatistics;

public record GetClubLeagueStatisticsQuery(int ClubId, DateOnly? Date = null, int? SeasonId = null) : IRequest<ClubLeagueStats?>;

public sealed class GetClubLeagueStatisticsHandler(IClubRepository clubs, ILogger<GetClubLeagueStatisticsHandler>? logger = null) : IRequestHandler<GetClubLeagueStatisticsQuery, ClubLeagueStats?>
{
  public async Task<ClubLeagueStats?> Handle(GetClubLeagueStatisticsQuery request, CancellationToken cancellationToken)
  {
    var stats = await clubs
      .GetCurrentClubLeagueStatsAsync(request.ClubId, request.Date, request.SeasonId, cancellationToken)
      .ConfigureAwait(false);
    if (stats == null)
    {
      logger?.LogWarning(
        "No league statistics found for club {ClubId} up to date {Date} in season {SeasonId}.",
        request.ClubId,
        request.Date,
        request.SeasonId);
    }

    return stats;
  }
}
