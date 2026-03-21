/**
 * League list item aligned with backend LeagueDto.
 * GET api/Database/leagues returns an array of these.
 */
export interface LeagueListItem {
  id: number;
  name: string;
  slug: string;
}

/**
 * League table row aligned with backend LeagueTableRowDto.
 */
export interface LeagueTableRowDto {
  position: number;
  clubId: number;
  clubName: string;
  matchesPlayed: number;
  wins: number;
  draws: number;
  losses: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  xg: number;
  xgDiff: number;
  xga: number;
  xgaDiff: number;
  xpts: number;
  xptsDiff: number;
}

/**
 * League table snapshot aligned with backend LeagueTableDto.
 * GET api/Database/leagues/{id}/table returns this.
 */
export interface LeagueTableDto {
  snapshotId: number;
  leagueId: number;
  seasonId: number;
  snapshotDate: string;
  leagueName: string;
  rows: LeagueTableRowDto[];
}
