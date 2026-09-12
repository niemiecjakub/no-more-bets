using MediatR;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;

namespace NoMoreBets.Application.Clubs.GetMatchRollingPerformancePair;

public record GetMatchRollingPerformancePairQuery(int MatchId) : IRequest<ClubPairDto<TeamPerformanceResult?>?>;

public sealed class GetMatchRollingPerformancePairHandler(IMatchRepository matches, IMediator mediator)
  : IRequestHandler<GetMatchRollingPerformancePairQuery, ClubPairDto<TeamPerformanceResult?>?>
{
  public async Task<ClubPairDto<TeamPerformanceResult?>?> Handle(
    GetMatchRollingPerformancePairQuery request,
    CancellationToken cancellationToken)
  {
    var match = await matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var asOfDate = DateOnly.FromDateTime(match.MatchDate);
    var home = await mediator
      .Send(new GetClubRollingPerformanceQuery(match.HomeClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);
    var away = await mediator
      .Send(new GetClubRollingPerformanceQuery(match.AwayClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);

    return new ClubPairDto<TeamPerformanceResult?>(home, away);
  }
}
