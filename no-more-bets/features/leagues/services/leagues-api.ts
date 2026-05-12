import axiosInstance from "../../../lib/axios";
import type { LeagueListItem } from "../interfaces";

/**
 * Fetches all leagues from the backend.
 */
export async function fetchLeagues(): Promise<LeagueListItem[]> {
  const { data } = await axiosInstance.get<LeagueListItem[]>(
    "/api/leagues"
  );
  return data;
}
