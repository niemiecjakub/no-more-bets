namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummary;

public record AgentDashboardBettingSummaryDto(
  int SettledSlipsCount,
  int SettledSelectionsCount,
  int WonSlipsCount,
  int LostSlipsCount,
  decimal WinRatePercent,
  decimal LossRatePercent);
