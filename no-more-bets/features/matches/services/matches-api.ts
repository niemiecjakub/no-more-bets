import { apiGet } from "../../../lib/api-client";
import type { MatchAnalysisPageDto, MatchListItem } from "../interfaces";

/**
 * Fetches all matches from the backend.
 */
export async function fetchMatches(): Promise<MatchListItem[]> {
  return apiGet<MatchListItem[]>("/api/Database/matches", "matches");
}

/**
 * Fetches match header and all analyses for a match.
 */
export async function fetchMatchAnalysisPage(
  matchId: number
): Promise<MatchAnalysisPageDto> {
  return apiGet<MatchAnalysisPageDto>(
    `/api/Database/matches/${matchId}/analyses`,
    "match analyses"
  );
}
