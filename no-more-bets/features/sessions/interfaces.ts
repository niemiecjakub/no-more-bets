/** Aligned with backend AgentSessionMatchSummaryDto. */
export interface AgentSessionMatchSummary {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  homeClubSlug: string;
  awayClubSlug: string;
  matchDate: string;
  matchStatusId: number;
  homeGoals: number | null;
  awayGoals: number | null;
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
  matchSummary?: AgentSessionMatchSummary | null;
}
