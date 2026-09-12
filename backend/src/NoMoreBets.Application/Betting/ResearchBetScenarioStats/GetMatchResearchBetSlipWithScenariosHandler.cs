using MediatR;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Betting.GetMatchResearchBetSlip;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.ResearchBetScenarioStats;

public record GetMatchResearchBetSlipWithScenariosQuery(int MatchId) : IRequest<MatchResearchBetSlipDto?>;

public sealed class GetMatchResearchBetSlipWithScenariosHandler(ISender sender)
  : IRequestHandler<GetMatchResearchBetSlipWithScenariosQuery, MatchResearchBetSlipDto?>
{
  public async Task<MatchResearchBetSlipDto?> Handle(
    GetMatchResearchBetSlipWithScenariosQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await sender
      .Send(new GetMatchResearchBetSlipQuery(request.MatchId), cancellationToken)
      .ConfigureAwait(false);

    if (slip is null)
    {
      return null;
    }

    // Hypothetical P&L is only meaningful once the slip has settled.
    var scenarios = slip.Status == BetStatus.Pending
      ? null
      : FromSummary(slip);

    return new MatchResearchBetSlipDto(slip, scenarios);
  }

  public static ResearchBetScenarioStatsDto FromSummary(BetSlipSummary slip)
  {
    var legs = slip.Selections
      .Select(s => new ResearchBetScenarioLegInput(s.OddsAtPlacement, s.Status))
      .ToList();
    var result = ResearchBetScenarioCalculator.Calculate(legs);

    return new ResearchBetScenarioStatsDto(
      ResearchBetScenarioCalculator.UnitStake,
      new ResearchBetParlayScenarioDto(
        result.Parlay.StakeTotal,
        result.Parlay.CombinedOdds,
        result.Parlay.PotentialPayout,
        result.Parlay.Profit),
      new ResearchBetSinglesScenarioDto(
        result.Singles.StakeTotal,
        result.Singles.PotentialPayout,
        result.Singles.Profit,
        result.Singles.Legs
          .Select(l => new ResearchBetSingleLegDto(l.Stake, l.Odds, l.Status, l.Profit))
          .ToList()));
  }
}
