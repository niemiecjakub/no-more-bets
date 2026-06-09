using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubBetSelectionStats;

public record GetClubBetSelectionStatsQuery(int ClubId) : IRequest<ClubBetSelectionStatsDto?>;

public sealed class GetClubBetSelectionStatsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetClubBetSelectionStatsQuery, ClubBetSelectionStatsDto?>
{
  public async Task<ClubBetSelectionStatsDto?> Handle(
    GetClubBetSelectionStatsQuery request,
    CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    var stats = await unitOfWork.Betting
      .GetResearchPhaseSettledSelectionStatsForClubAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    return new ClubBetSelectionStatsDto(stats.WonCount, stats.LostCount, stats.TotalCount);
  }
}
