using NoMoreBets.Application.Betting.GetBetSlips;

namespace NoMoreBets.Application.Betting.ResearchBetScenarioStats;

public interface IResearchBetScenarioStatsService
{
  ResearchBetScenarioStatsDto FromSummary(BetSlipSummary slip);
}
