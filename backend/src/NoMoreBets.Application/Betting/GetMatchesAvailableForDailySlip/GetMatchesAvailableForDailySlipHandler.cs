using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Betting.GetMatchesAvailableForDailySlip;

public record GetMatchesAvailableForDailySlipQuery(DateTime UtcNow) : IRequest<IReadOnlyList<Match>>;

public sealed class GetMatchesAvailableForDailySlipHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMatchesAvailableForDailySlipQuery, IReadOnlyList<Match>>
{
  public async Task<IReadOnlyList<Match>> Handle(
    GetMatchesAvailableForDailySlipQuery request,
    CancellationToken cancellationToken)
  {
    var matches = await unitOfWork.Betting
      .GetMatchesAvailableForBettingAsync(cancellationToken)
      .ConfigureAwait(false);

    var cardDate = DateOnly.FromDateTime(request.UtcNow);
    return matches
      .Where(m => DateOnly.FromDateTime(m.MatchDate) == cardDate)
      .ToList();
  }
}
