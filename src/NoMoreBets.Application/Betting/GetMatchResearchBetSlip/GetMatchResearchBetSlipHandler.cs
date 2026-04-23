using MediatR;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Betting.GetMatchResearchBetSlip;

public record GetMatchResearchBetSlipQuery(int MatchId) : IRequest<BetSlipSummary?>;

public sealed class GetMatchResearchBetSlipHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMatchResearchBetSlipQuery, BetSlipSummary?>
{
  public async Task<BetSlipSummary?> Handle(
    GetMatchResearchBetSlipQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await unitOfWork.Betting
      .GetLatestResearchBetSlipForMatchAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaryOrNull(slip);
  }
}
