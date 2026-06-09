namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardSessions;

public record AgentDashboardSessionsDto(
  int SessionsCount,
  DateTime? LatestStartedAt,
  string? LatestPhaseName);
