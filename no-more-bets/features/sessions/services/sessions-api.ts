import axiosInstance from "../../../lib/axios";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import type { AgentSessionListItem } from "../interfaces";

const AGENT_SESSIONS_PAGE_SIZE = 15;

export interface FetchAgentSessionsPageParams {
  limit?: number;
  afterStartedAt?: string;
  afterId?: number;
  includeSessionId?: number;
}

/**
 * Fetches a page of agent sessions from the backend (newest first).
 */
export async function fetchAgentSessionsPage(
  params: FetchAgentSessionsPageParams = {},
): Promise<PagedResponse<AgentSessionListItem>> {
  const { data } = await axiosInstance.get<unknown>("/api/agent-sessions", {
    params: {
      limit: params.limit ?? AGENT_SESSIONS_PAGE_SIZE,
      afterStartedAt: params.afterStartedAt,
      afterId: params.afterId,
      includeSessionId: params.includeSessionId,
    },
  });
  return normalizePagedResponse(data, (item) => item as AgentSessionListItem);
}
