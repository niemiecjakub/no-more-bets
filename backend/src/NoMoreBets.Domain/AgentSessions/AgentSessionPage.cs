namespace NoMoreBets.Domain.AgentSessions;

public sealed record AgentSessionPage(IReadOnlyList<AgentSessionListRow> Items, bool HasMore);

public sealed record AgentSessionListRow(
  int Id,
  AgentSessionPhase Phase,
  DateTime StartedAt,
  int MessageCount);
