namespace NoMoreBets.Domain.AgentSessions;

public sealed record AgentSessionsWidgetData(
  int SessionsCount,
  DateTime? LatestStartedAt,
  string? LatestPhaseName);
