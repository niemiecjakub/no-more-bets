using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Betting.GetMatchesAvailableForBetting;

public record GetMatchesAvailableForBettingQuery : IRequest<IReadOnlyList<Match>>;

public sealed class GetMatchesAvailableForBettingHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMatchesAvailableForBettingQuery, IReadOnlyList<Match>>
{
  public async Task<IReadOnlyList<Match>> Handle(
    GetMatchesAvailableForBettingQuery _,
    CancellationToken cancellationToken)
  {
    var matches = await unitOfWork.Betting
      .GetMatchesAvailableForBettingAsync(cancellationToken)
      .ConfigureAwait(false);

    return matches;
  }
}
