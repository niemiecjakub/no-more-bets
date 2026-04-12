import axiosInstance from "../../../lib/axios";
import type { AgentSessionListItem } from "../interfaces";

/**
 * Fetches all agent sessions from the backend (newest first).
 */
export async function fetchAgentSessions(): Promise<AgentSessionListItem[]> {
  const { data } = await axiosInstance.get<AgentSessionListItem[]>(
    "/api/Database/agent-sessions"
  );
  return data;
}
