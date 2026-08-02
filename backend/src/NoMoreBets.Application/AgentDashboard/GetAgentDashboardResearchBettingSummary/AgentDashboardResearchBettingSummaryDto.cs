namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;

public record ResearchScenarioPnlDto(
  decimal StakeTotal,
  decimal Profit,
  decimal Roi);

public record AgentDashboardResearchBettingSummaryDto(
  int SettledSelectionsCount,
  int WonSelectionsCount,
  int LostSelectionsCount,
  decimal WinRatePercent,
  decimal LossRatePercent,
  decimal UnitStake,
  int ScenarioSlipCount,
  ResearchScenarioPnlDto Parlay,
  ResearchScenarioPnlDto Singles);
