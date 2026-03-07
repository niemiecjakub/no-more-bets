import { apiGet } from "../../../lib/api-client";
import type { LeagueListItem, LeagueTableDto } from "../interfaces";

/**
 * Fetches all leagues from the backend.
 */
export async function fetchLeagues(): Promise<LeagueListItem[]> {
  return apiGet<LeagueListItem[]>("/api/Database/leagues", "leagues");
}

/**
 * Fetches the latest league table for the given league.
 * Throws on failure (e.g. 404 when no snapshot exists).
 */
export async function fetchLeagueTable(leagueId: number): Promise<LeagueTableDto> {
  return apiGet<LeagueTableDto>(
    `/api/Database/leagues/${leagueId}/table`,
    "league table"
  );
}
