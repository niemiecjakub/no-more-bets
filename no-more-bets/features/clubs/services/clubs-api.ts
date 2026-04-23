import axiosInstance from "../../../lib/axios";
import type { ClubListItem } from "../interfaces";

/**
 * Fetches all clubs from the backend.
 */
export async function fetchClubs(): Promise<ClubListItem[]> {
  const { data } = await axiosInstance.get<ClubListItem[]>(
    "/api/clubs"
  );
  return data;
}
