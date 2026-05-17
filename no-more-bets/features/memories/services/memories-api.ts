import axiosInstance from "../../../lib/axios";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import type { MemoryListItem } from "../interfaces";

const MEMORIES_PAGE_SIZE = 15;

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
): Promise<PagedResponse<MemoryListItem>> {
  const { data } = await axiosInstance.get<unknown>("/api/memories", {
    params: {
      limit: params.limit ?? MEMORIES_PAGE_SIZE,
      afterUpdatedAt: params.afterUpdatedAt,
      afterId: params.afterId,
    },
  });
  return normalizePagedResponse(data, (item) => item as MemoryListItem);
}
