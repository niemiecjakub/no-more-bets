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
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string LeagueName,
  string LeagueSlug,
  string SeasonYear,
  int MatchStatusId,
  int? HomeGoals,
  int? AwayGoals,
  DateTime MatchDate,
  IReadOnlyList<MatchAnalysisItemDto> Analyses,
  int? ResearchAgentSessionId);
