import axiosInstance from "../../../lib/axios";
import type { MatchAnalysisPageDto, MatchListItem } from "../interfaces";

/**
 * Fetches all matches from the backend.
 */
export async function fetchMatches(): Promise<MatchListItem[]> {
  const { data } = await axiosInstance.get<MatchListItem[]>(
    "/api/Database/matches"
  );
  return data;
}

/**
 * Fetches match header and all analyses for a match.
 */
export async function fetchMatchAnalysisPage(
  matchId: number
): Promise<MatchAnalysisPageDto> {
  const { data } = await axiosInstance.get<MatchAnalysisPageDto>(
    `/api/Database/matches/${matchId}/analyses`
  );
  return data;
}
