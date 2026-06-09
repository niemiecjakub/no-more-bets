namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardBettingSummaryDetails;

public record AgentDashboardBettingSummaryDetailsDto(
  int WonSlipsCount,
  int LostSlipsCount,
  int WonSelectionsCount,
  int LostSelectionsCount);
