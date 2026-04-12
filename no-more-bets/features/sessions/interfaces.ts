/** Aligned with backend AgentSessionListItemDto. */
export interface AgentSessionListItem {
  id: number;
  phaseId: number;
  phaseName: string;
  startedAt: string;
  messageCount: number;
}
