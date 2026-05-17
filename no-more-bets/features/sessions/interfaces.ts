/** GET api/agent-sessions */
export interface AgentSessionsPage {
  items: AgentSessionListItem[];
  hasMore: boolean;
  nextCursorStartedAt: string | null;
  nextCursorId: number | null;
}

/** Aligned with backend AgentSessionListItemDto. */
export interface AgentSessionListItem {
  id: number;
  phaseId: number;
  phaseName: string;
  startedAt: string;
  /** Excludes function-call (tool) transcript rows. */
  messageCount: number;
  /** Present when this session is tied to match research (`MatchAnalysis`). */
  matchId?: number | null;
}
