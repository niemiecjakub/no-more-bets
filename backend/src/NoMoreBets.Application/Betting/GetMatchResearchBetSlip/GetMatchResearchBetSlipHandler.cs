using MediatR;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Betting.GetBetSlips;

namespace NoMoreBets.Application.Betting.GetMatchResearchBetSlip;

public record GetMatchResearchBetSlipQuery(int MatchId) : IRequest<BetSlipSummary?>;

public sealed class GetMatchResearchBetSlipHandler(IBettingRepository betting)
  : IRequestHandler<GetMatchResearchBetSlipQuery, BetSlipSummary?>
{
  public async Task<BetSlipSummary?> Handle(
    GetMatchResearchBetSlipQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await betting
      .GetLatestResearchBetSlipForMatchAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaryOrNull(slip);
  }
}
