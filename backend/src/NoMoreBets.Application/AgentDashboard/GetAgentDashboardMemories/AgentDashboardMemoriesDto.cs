namespace NoMoreBets.Application.AgentDashboard.GetAgentDashboardMemories;

public record AgentDashboardMemoriesDto(
  int MemoriesCount,
  DateTime? LatestUpdatedAt,
  string? LatestName);
