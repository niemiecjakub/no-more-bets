using MediatR;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Application.Clubs.Common;
using NoMoreBets.Application.Clubs.GetClubRecentGames;

namespace NoMoreBets.Application.Clubs.GetMatchRecentGamesPair;

public record GetMatchRecentGamesPairQuery(int MatchId) : IRequest<ClubPairDto<IReadOnlyList<RecentMatch>?>?>;

public sealed class GetMatchRecentGamesPairHandler(IMatchRepository matches, IMediator mediator)
  : IRequestHandler<GetMatchRecentGamesPairQuery, ClubPairDto<IReadOnlyList<RecentMatch>?>?>
{
  public async Task<ClubPairDto<IReadOnlyList<RecentMatch>?>?> Handle(
    GetMatchRecentGamesPairQuery request,
    CancellationToken cancellationToken)
  {
    var match = await matches
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
