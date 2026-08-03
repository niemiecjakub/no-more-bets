import axiosInstance from "../../../lib/axios";
import type { LeagueListItem, LeagueTable, SeasonYearItem } from "../interfaces";

/**
 * Fetches all leagues from the backend.
 */
export async function fetchLeagues(): Promise<LeagueListItem[]> {
  const { data } = await axiosInstance.get<LeagueListItem[]>(
    "/api/leagues"
  );
  return data;
}

/**
 * Distinct season years ordered latest-first.
 */
export async function fetchSeasonYears(): Promise<SeasonYearItem[]> {
  const { data } = await axiosInstance.get<SeasonYearItem[]>("/api/seasons");
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
