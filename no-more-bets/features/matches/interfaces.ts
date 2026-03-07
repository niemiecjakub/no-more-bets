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
}

/** MatchStatusId from backend enum: Upcomming = 1, Finished = 2 */
export const MATCH_STATUS = {
  Upcoming: 1,
  Finished: 2,
} as const;
