import { apiGet } from "../../../lib/api-client";
import type { LeagueListItem } from "../interfaces";

/**
 * Fetches all leagues from the backend.
 */
export async function fetchLeagues(): Promise<LeagueListItem[]> {
  return apiGet<LeagueListItem[]>("/api/Database/leagues", "leagues");
}
