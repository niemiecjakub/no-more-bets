namespace NoMoreBets.Application.AgentSessions.GetAgentSessionsPage;

public record AgentSessionMatchSummaryDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  DateTime MatchDate,
  int MatchStatusId,
  int? HomeGoals,
  int? AwayGoals);

/// <param name="MessageCount">Transcript messages only; excludes function-call (tool) rows.</param>
public record AgentSessionListItemDto(
  int Id,
  int PhaseId,
  string PhaseName,
  DateTime StartedAt,
  int MessageCount,
  int? MatchId = null,
  AgentSessionMatchSummaryDto? MatchSummary = null);
