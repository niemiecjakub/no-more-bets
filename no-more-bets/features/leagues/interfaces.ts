/**
 * League list item aligned with backend LeagueDto.
 * GET api/leagues returns an array of these.
 */
/** Matches backend `MatchResult` (serialized as enum name or 0/1/2). */
export type MatchResult = "Win" | "Draw" | "Loss";

export interface LeagueListItem {
  id: number;
  name: string;
  slug: string;
}

export interface LeagueTableRow {
  position: number;
  clubId: number;
  clubName: string;
  clubSlug: string;
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
  form: MatchResult[];
}

export interface WorldCupGroupTable {
  groupCode: string;
  groupLabel: string;
  rows: LeagueTableRow[];
}

export interface LeagueTable {
  snapshotId: number;
  leagueId: number;
  seasonId: number;
  snapshotDate: string;
  leagueName: string;
  leagueSlug: string;
  rows: LeagueTableRow[];
  ownGroupCode?: string | null;
  groups?: WorldCupGroupTable[] | null;
}
