import axiosInstance from "../../../lib/axios";

/** Aligned with backend AgentSessionMessageDto. */
export interface AgentSessionMessage {
  id: number;
  sessionId: number;
  ordinal: number;
  kind: number;
  text: string;
}

/**
 * Fetches ordered transcript messages for an agent session.
 */
export async function fetchAgentSessionMessages(
  sessionId: number
): Promise<AgentSessionMessage[]> {
  const { data } = await axiosInstance.get<AgentSessionMessage[]>(
    `/api/Database/agent-sessions/${sessionId}/messages`
  );
  return data;
}
