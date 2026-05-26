import axiosInstance from "../../../lib/axios";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import type { AgentSessionListItem } from "../interfaces";

const AGENT_SESSIONS_PAGE_SIZE = 15;

export interface FetchAgentSessionsPageParams {
  limit?: number;
  afterStartedAt?: string;
  afterId?: number;
  includeSessionId?: number;
  phaseIds?: number[];
}

/**
 * Fetches a page of agent sessions from the backend (newest first).
 */
export async function fetchAgentSessionsPage(
  params: FetchAgentSessionsPageParams = {},
): Promise<PagedResponse<AgentSessionListItem>> {
  const queryParams = new URLSearchParams();
  queryParams.set("limit", String(params.limit ?? AGENT_SESSIONS_PAGE_SIZE));

  if (params.afterStartedAt != null) {
    queryParams.set("afterStartedAt", params.afterStartedAt);
  }
  if (params.afterId != null) {
    queryParams.set("afterId", String(params.afterId));
  }
  if (params.includeSessionId != null) {
    queryParams.set("includeSessionId", String(params.includeSessionId));
  }
  for (const phaseId of params.phaseIds ?? []) {
    if (Number.isInteger(phaseId)) {
      queryParams.append("phaseIds", String(phaseId));
    }
  }

  const { data } = await axiosInstance.get<unknown>(
    `/api/agent-sessions?${queryParams.toString()}`,
  );
  return normalizePagedResponse(data, (item) => item as AgentSessionListItem);
}
