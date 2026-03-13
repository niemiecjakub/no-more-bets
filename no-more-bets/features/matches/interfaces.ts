/**
 * Match list item aligned with backend MatchDto.
 * GET api/Database/matches returns an array of these.
 */
export interface MatchListItem {
  id: number;
  matchDate: string;
  homeClubId: number;
  awayClubId: number;
  homeClubName: string;
  awayClubName: string;
  matchStatusId: number;
  matchStatusName: string;
  homeGoals: number | null;
  awayGoals: number | null;
  betclicUrl: string | null;
  isReadyToPredict: boolean;
  hasAnalysis: boolean;
}

/** MatchStatusId from backend enum: Upcomming = 1, Finished = 2 */
export const MATCH_STATUS = {
  Upcoming: 1,
  Finished: 2,
} as const;

/** Structured analysis sections returned when Content is valid JSON. */
export interface StructuredMatchAnalysis {
  context?: string | null;
  form?: string | null;
  tactics?: string | null;
  squad?: string | null;
  statistics?: string | null;
  market?: string | null;
  matchProjection?: string | null;
  prediction?: string | null;
}

/** Single analysis item from GET api/Database/matches/:id/analyses */
export interface MatchAnalysisItemDto {
  id: number;
  code: string;
  content: string;
  structured?: StructuredMatchAnalysis | null;
}

/** Match analysis page payload: match header + analyses list */
export interface MatchAnalysisPageDto {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  matchDate: string;
  analyses: MatchAnalysisItemDto[];
}
