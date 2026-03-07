import { apiGet } from "../../../lib/api-client";
import type { ClubListItem } from "../interfaces";

/**
 * Fetches all clubs from the backend.
 */
export async function fetchClubs(): Promise<ClubListItem[]> {
  return apiGet<ClubListItem[]>("/api/Database/clubs", "clubs");
}
