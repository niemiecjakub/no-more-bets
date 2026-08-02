using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.Betting.ResearchBetScenarioStats;

public sealed class ResearchBetScenarioStatsService : IResearchBetScenarioStatsService
{
  public ResearchBetScenarioStatsDto FromSummary(BetSlipSummary slip)
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
