using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;

namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;

public record GetAgentDashboardResearchBettingSummaryQuery(IReadOnlyList<int> LeagueIds)
  : IRequest<AgentDashboardResearchBettingSummaryDto>;

public sealed class GetAgentDashboardResearchBettingSummaryHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetAgentDashboardResearchBettingSummaryQuery, AgentDashboardResearchBettingSummaryDto>
{
  public async Task<AgentDashboardResearchBettingSummaryDto> Handle(
    GetAgentDashboardResearchBettingSummaryQuery request,
    CancellationToken cancellationToken)
  {
    var stats = await unitOfWork.Betting
      .GetResearchPhaseSettledSummaryAsync(request.LeagueIds, cancellationToken)
      .ConfigureAwait(false);

    var legs = await unitOfWork.Betting
      .GetResearchPhaseSettledScenarioLegsAsync(request.LeagueIds, cancellationToken)
      .ConfigureAwait(false);

    var scenarios = AggregateScenarios(legs);

    var winRate = stats.SettledSelectionsCount == 0
      ? 0m
      : (decimal)stats.WonSelectionsCount / stats.SettledSelectionsCount * 100m;
    var lossRate = stats.SettledSelectionsCount == 0
      ? 0m
      : (decimal)stats.LostSelectionsCount / stats.SettledSelectionsCount * 100m;

    return new AgentDashboardResearchBettingSummaryDto(
      stats.SettledSelectionsCount,
      stats.WonSelectionsCount,
      stats.LostSelectionsCount,
      winRate,
      lossRate,
      ResearchBetScenarioCalculator.UnitStake,
      scenarios.SlipCount,
      scenarios.Parlay,
      scenarios.Singles);
  }

  private static (int SlipCount, ResearchScenarioPnlDto Parlay, ResearchScenarioPnlDto Singles) AggregateScenarios(
    IReadOnlyList<ResearchPhaseScenarioLegRow> legs)
  {
    decimal parlayStake = 0m;
    decimal parlayProfit = 0m;
    decimal singlesStake = 0m;
    decimal singlesProfit = 0m;
    var slipCount = 0;

    foreach (var group in legs.GroupBy(l => l.SlipId))
    {
      var inputs = group
        .Select(l => new ResearchBetScenarioLegInput(l.OddsAtPlacement, l.Status))
        .ToList();
      var result = ResearchBetScenarioCalculator.Calculate(inputs);

      if (result.Parlay.Profit is null || result.Singles.Profit is null)
      {
        continue;
      }

      parlayStake += result.Parlay.StakeTotal;
      parlayProfit += result.Parlay.Profit.Value;
      singlesStake += result.Singles.StakeTotal;
      singlesProfit += result.Singles.Profit.Value;
      slipCount++;
    }

    return (slipCount, ToPnl(parlayStake, parlayProfit), ToPnl(singlesStake, singlesProfit));
  }

  private static ResearchScenarioPnlDto ToPnl(decimal stakeTotal, decimal profit) =>
    new(
      stakeTotal,
      profit,
      stakeTotal == 0m ? 0m : Math.Round(profit / stakeTotal, 4));
}
