using MediatR;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetMatchRecentGamesPair;

public record GetMatchRecentGamesPairQuery(int MatchId) : IRequest<ClubPairDto<IReadOnlyList<RecentMatch>?>?>;

public sealed class GetMatchRecentGamesPairHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetMatchRecentGamesPairQuery, ClubPairDto<IReadOnlyList<RecentMatch>?>?>
{
  public async Task<ClubPairDto<IReadOnlyList<RecentMatch>?>?> Handle(
    GetMatchRecentGamesPairQuery request,
    CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var asOfDate = DateOnly.FromDateTime(match.MatchDate);
    var home = await mediator
      .Send(new GetClubRecentGamesQuery(match.HomeClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);
    var away = await mediator
      .Send(new GetClubRecentGamesQuery(match.AwayClubId, asOfDate), cancellationToken)
      .ConfigureAwait(false);

    return new ClubPairDto<IReadOnlyList<RecentMatch>?>(home, away);
  }
}
