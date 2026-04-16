using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Leagues.GetClubLeagueStatistics;

public record GetClubLeagueStatisticsQuery(int ClubId, DateOnly? Date = null) : IRequest<ClubLeagueStats?>;

public sealed class GetClubLeagueStatisticsHandler(IUnitOfWork unitOfWork, ILogger<GetClubLeagueStatisticsHandler>? logger = null) : IRequestHandler<GetClubLeagueStatisticsQuery, ClubLeagueStats?>
{
  public async Task<ClubLeagueStats?> Handle(GetClubLeagueStatisticsQuery request, CancellationToken cancellationToken)
  {
    var stats = await unitOfWork.Clubs.GetCurrentClubLeagueStatsAsync(request.ClubId, request.Date, cancellationToken).ConfigureAwait(false);
    if (stats == null)
    {
      logger?.LogWarning("No league statistics found for club {ClubId} up to date {Date}.", request.ClubId, request.Date);
    }

    return stats;
  }
}
