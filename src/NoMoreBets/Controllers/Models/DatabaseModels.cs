namespace NoMoreBets.Controllers.Models;

public record MemoryListItemDto(
  int Id,
  string Name,
  string? Description,
  string Content,
  DateTime CreatedAt,
  DateTime UpdatedAt);

public record LeagueDto(int Id, string Name, string Slug);

public record ClubDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);

public record MatchDto(
  int Id,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string LeagueName,
  string LeagueSlug,
  int MatchStatusId,
  string MatchStatusName,
  int? HomeGoals,
  int? AwayGoals,
  string? BetclicUrl,
  bool IsReadyToPredict = false,
  bool HasAnalysis = false,
  bool HasResearch = false,
  bool HasPreview = false,
  bool HasLineup = false,
  bool HasOdds = false,
  bool HasHeadToHead = false);

public record LeagueTableDto(
  long SnapshotId,
  int LeagueId,
  int SeasonId,
  DateOnly SnapshotDate,
  string LeagueName,
  string LeagueSlug,
  IReadOnlyList<LeagueTableRowDto> Rows);

public record LeagueTableRowDto(
  int Position,
  int ClubId,
  string ClubName,
  string ClubSlug,
  int MatchesPlayed,
  int Wins,
  int Draws,
  int Losses,
  int GoalsFor,
  int GoalsAgainst,
  int GoalDifference,
  int Points,
  decimal Xg,
  decimal XgDiff,
  decimal Xga,
  decimal XgaDiff,
  decimal Xpts,
  decimal XptsDiff);

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

public record BetSelectionItemDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string EventTypeName,
  string EventOptionName,
  decimal OddsAtPlacement,
  int StatusId,
  string StatusName);

public record BetSlipListItemDto(
  int Id,
  DateTime CreatedAt,
  decimal StakeAmount,
  decimal TotalOdds,
  decimal PotentialPayout,
  int StatusId,
  string StatusName,
  IReadOnlyList<BetSelectionItemDto> Selections,
  int? AgentSessionId);

public record AgentSessionListItemDto(
  int Id,
  int PhaseId,
  string PhaseName,
  DateTime StartedAt,
  int MessageCount);

public record AgentSessionMessageDto(int Id, int SessionId, int Ordinal, int Kind, string Text);
