using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Leagues.GetClubLeagueStatistics;

public record GetClubLeagueStatisticsQuery(int ClubId, DateOnly? Date = null) : IRequest<ClubLeagueStats?>;

public sealed class GetClubLeagueStatisticsHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetClubLeagueStatisticsQuery, ClubLeagueStats?>
{
  public async Task<ClubLeagueStats?> Handle(GetClubLeagueStatisticsQuery request, CancellationToken cancellationToken)
  {
    return await unitOfWork.Clubs.GetCurrentClubLeagueStatsAsync(request.ClubId, request.Date, cancellationToken).ConfigureAwait(false);
  }
}
