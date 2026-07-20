import axiosInstance from "../../../lib/axios";
import type { LeagueListItem, LeagueTable } from "../interfaces";

/**
 * Fetches all leagues from the backend.
 */
export async function fetchLeagues(): Promise<LeagueListItem[]> {
  const { data } = await axiosInstance.get<LeagueListItem[]>(
    "/api/leagues"
  );
  return data;
}

export async function fetchLeagueTable(
  leagueId: number,
  seasonId: number,
  clubId?: number,
): Promise<LeagueTable> {
  const { data } = await axiosInstance.get<LeagueTable>(
    `/api/leagues/${leagueId}/table`,
    { params: { seasonId, ...(clubId != null ? { clubId } : {}) } },
  );
  return data;
}
