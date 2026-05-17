import axiosInstance from "../../../lib/axios";
import type { AgentSessionsPage } from "../interfaces";

const AGENT_SESSIONS_PAGE_SIZE = 25;

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
): Promise<AgentSessionsPage> {
  const { data } = await axiosInstance.get<AgentSessionsPage>("/api/agent-sessions", {
    params: {
      limit: params.limit ?? AGENT_SESSIONS_PAGE_SIZE,
      afterStartedAt: params.afterStartedAt,
      afterId: params.afterId,
      includeSessionId: params.includeSessionId,
    },
  });
  return data;
}
