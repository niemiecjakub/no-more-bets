namespace NoMoreBets.Application.AgentSessions.GetAgentSessionsPage;

/// <param name="MessageCount">Transcript messages only; excludes function-call (tool) rows.</param>
public record AgentSessionListItemDto(
  int Id,
  int PhaseId,
  string PhaseName,
  DateTime StartedAt,
  int MessageCount,
  int? MatchId = null);
