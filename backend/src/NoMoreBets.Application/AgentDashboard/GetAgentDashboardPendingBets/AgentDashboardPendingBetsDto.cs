namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardPendingBets;

public record AgentDashboardPendingBetsDto(
  int PendingSlipsCount,
  decimal PendingStakeTotal,
  decimal PendingPotentialPayoutTotal,
  DateTime? LatestPendingCreatedAt);
