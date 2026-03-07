import { apiGet } from "../../../lib/api-client";
import type { MatchListItem } from "../interfaces";

/**
 * Fetches all matches from the backend.
 */
export async function fetchMatches(): Promise<MatchListItem[]> {
  return apiGet<MatchListItem[]>("/api/Database/matches", "matches");
}
