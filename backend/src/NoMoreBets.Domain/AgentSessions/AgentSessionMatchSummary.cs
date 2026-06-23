namespace NoMoreBets.Domain.AgentSessions;

public record AgentSessionMatchSummary(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  DateTime MatchDate,
  int MatchStatusId,
  int? HomeGoals,
  int? AwayGoals);
