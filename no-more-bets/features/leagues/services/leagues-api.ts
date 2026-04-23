import axiosInstance from "../../../lib/axios";
import type { LeagueListItem, LeagueTableDto } from "../interfaces";

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
 * Fetches the latest league table for the given league.
 */
export async function fetchLeagueTable(
  leagueId: number
): Promise<LeagueTableDto> {
  const { data } = await axiosInstance.get<LeagueTableDto>(
    `/api/leagues/${leagueId}/table`
  );
  return data;
}
