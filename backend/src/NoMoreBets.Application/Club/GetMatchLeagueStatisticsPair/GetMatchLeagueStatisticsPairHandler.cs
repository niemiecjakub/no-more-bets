using MediatR;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Clubs.GetMatchLeagueStatisticsPair;

public record GetMatchLeagueStatisticsPairQuery(int MatchId) : IRequest<ClubPairDto<ClubLeagueStats?>?>;

public sealed class GetMatchLeagueStatisticsPairHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetMatchLeagueStatisticsPairQuery, ClubPairDto<ClubLeagueStats?>?>
{
  public async Task<ClubPairDto<ClubLeagueStats?>?> Handle(
    GetMatchLeagueStatisticsPairQuery request,
    CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var asOfDate = DateOnly.FromDateTime(match.MatchDate);
    var home = await mediator
      .Send(new GetClubLeagueStatisticsQuery(match.HomeClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);
    var away = await mediator
      .Send(new GetClubLeagueStatisticsQuery(match.AwayClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);

    return new ClubPairDto<ClubLeagueStats?>(home, away);
  }
}
