import axiosInstance from "../../../lib/axios";
import type { MemoryListItem } from "../interfaces";

/**
 * Fetches all saved memory records from the backend (ordered by name).
 */
export async function fetchMemories(): Promise<MemoryListItem[]> {
  const { data } = await axiosInstance.get<MemoryListItem[]>(
    "/api/memories"
  );
  return data;
}
