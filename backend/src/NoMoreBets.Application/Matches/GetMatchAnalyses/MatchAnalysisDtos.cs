namespace NoMoreBets.Application.Matches.GetMatchAnalyses;

public record StructuredMatchAnalysisDto(
  string? Context,
  string? Form,
  string? Tactics,
  string? Squad,
  string? Statistics,
  string? Market,
  string? MatchProjection,
  string? Prediction);

public record MatchAnalysisItemDto(
  int Id,
  string Code,
  string Content,
  StructuredMatchAnalysisDto? Structured);

public record MatchAnalysisPageDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  int MatchStatusId,
  int? HomeGoals,
  int? AwayGoals,
  DateTime MatchDate,
  IReadOnlyList<MatchAnalysisItemDto> Analyses,
  int? ResearchAgentSessionId);
