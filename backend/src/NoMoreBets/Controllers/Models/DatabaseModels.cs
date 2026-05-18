namespace NoMoreBets.Controllers.Models;

public record LeagueDto(int Id, string Name, string Slug);

public record ClubDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);

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

/// <param name="MessageCount">Transcript messages only; excludes function-call (tool) rows.</param>
public record AgentSessionListItemDto(
  int Id,
  int PhaseId,
  string PhaseName,
  DateTime StartedAt,
  int MessageCount,
  int? MatchId = null);

public record AgentSessionMessageDto(int Id, int SessionId, int Ordinal, int Kind, string Text);
