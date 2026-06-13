import axiosInstance from "../../../lib/axios";

/** Aligned with backend ToolCallDisplayDto. */
export interface ToolCallDisplay {
  label: string;
  category: string;
  details: string[] | null;
}

/** Aligned with backend AgentSessionMessageDto. */
export interface AgentSessionMessage {
  id: number;
  sessionId: number;
  ordinal: number;
  kind: number;
  text: string;
  toolCallDisplay?: ToolCallDisplay | null;
}

/**
 * Fetches ordered transcript messages for an agent session.
 */
export async function fetchAgentSessionMessages(
  sessionId: number
): Promise<AgentSessionMessage[]> {
  const { data } = await axiosInstance.get<AgentSessionMessage[]>(
    `/api/agent-sessions/${sessionId}/messages`
  );
  return data;
}
