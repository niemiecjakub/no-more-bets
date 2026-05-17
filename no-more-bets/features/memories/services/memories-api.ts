import axiosInstance from "../../../lib/axios";
import type { MemoriesPage } from "../interfaces";

const MEMORIES_PAGE_SIZE = 25;

export interface FetchMemoriesPageParams {
  limit?: number;
  afterUpdatedAt?: string;
  afterId?: number;
}

/**
 * Fetches a page of saved memory records from the backend (newest updated first).
 */
export async function fetchMemoriesPage(
  params: FetchMemoriesPageParams = {},
): Promise<MemoriesPage> {
  const { data } = await axiosInstance.get<MemoriesPage>("/api/memories", {
    params: {
      limit: params.limit ?? MEMORIES_PAGE_SIZE,
      afterUpdatedAt: params.afterUpdatedAt,
      afterId: params.afterId,
    },
  });
  return data;
}
