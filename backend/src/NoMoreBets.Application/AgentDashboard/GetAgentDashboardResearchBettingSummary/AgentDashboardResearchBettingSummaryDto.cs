namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardResearchBettingSummary;

public record AgentDashboardResearchBettingSummaryDto(
  int SettledSelectionsCount,
  int WonSelectionsCount,
  int LostSelectionsCount,
  decimal WinRatePercent,
  decimal LossRatePercent);
