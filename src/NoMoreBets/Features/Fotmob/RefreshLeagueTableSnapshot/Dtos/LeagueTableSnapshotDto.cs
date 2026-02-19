namespace NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot.Dtos;

/// <summary>DTO for a league table snapshot (metadata + rows).</summary>
public record LeagueTableSnapshotDto(
  long Id,
  int LeagueId,
  int SeasonId,
  DateOnly SnapshotDate,
  DateTime CreatedAt,
  IReadOnlyList<LeagueTableSnapshotRowDto> Rows);

/// <summary>DTO for a single row in a league table snapshot.</summary>
public record LeagueTableSnapshotRowDto(
  int ClubId,
  string ClubName,
  int Position,
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
