using MediatR;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Clubs.GetClubBetSelectionStats;

public record GetClubBetSelectionStatsQuery(int ClubId) : IRequest<ClubBetSelectionStatsDto?>;

public sealed class GetClubBetSelectionStatsHandler(IBettingRepository betting, IClubRepository clubs)
  : IRequestHandler<GetClubBetSelectionStatsQuery, ClubBetSelectionStatsDto?>
{
  public async Task<ClubBetSelectionStatsDto?> Handle(
    GetClubBetSelectionStatsQuery request,
    CancellationToken cancellationToken)
  {
    var club = await clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    var stats = await betting
      .GetResearchPhaseSettledSelectionStatsForClubAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    return new ClubBetSelectionStatsDto(stats.WonCount, stats.LostCount, stats.TotalCount);
  }
}
