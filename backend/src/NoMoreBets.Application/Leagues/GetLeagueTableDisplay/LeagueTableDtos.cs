using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Leagues.GetLeagueTableDisplay;

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
  decimal XptsDiff,
  IReadOnlyList<MatchResult> Form);
