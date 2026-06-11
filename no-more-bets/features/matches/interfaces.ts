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
  homeClubSlug: string;
  awayClubSlug: string;
  /** Competition name from match stage / season / league (empty if unknown). */
  leagueName: string;
  /** Slug for `/leagues/{slug}.svg` (empty if unknown). */
  leagueSlug: string;
  matchStatusId: number;
  matchStatusName: string;
  homeGoals: number | null;
  awayGoals: number | null;
  betclicUrl: string | null;
  isReadyToPredict: boolean;
  hasResearch: boolean;
  hasResearchBet: boolean;
  hasLineup: boolean;
  hasOdds: boolean;
  hasHeadToHead: boolean;
}

/** MatchStatusId from backend enum: Upcomming = 1, Finished = 2 */
export const MATCH_STATUS = {
  Upcoming: 1,
  Finished: 2,
} as const;

/** Structured agent research output from GET api/matchinsights/matches/:id/agent-research */
export interface MatchResearchOutput {
  matchOverview: string;
  keyPoints: string[];
  risksAndUnknowns: string[];
}

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
  homeClubId: number;
  awayClubId: number;
  homeClubName: string;
  awayClubName: string;
  homeClubSlug?: string;
  awayClubSlug?: string;
  matchStatusId: number;
  homeGoals: number | null;
  awayGoals: number | null;
  matchDate: string;
  analyses: MatchAnalysisItemDto[];
  researchAgentSessionId: number | null;
  matchDetails?: MatchDetailsSummary | null;
}

export interface MatchDetailsSummary {
  fotmobUrl: string | null;
  fotmobDetailsJson: string | null;
  fotmobReview: string | null;
}

export interface Player {
  name: string;
  position: string;
}

export interface TeamLineupResult {
  lineupType: string;
  players: Player[];
}

export interface MatchLineupResult {
  home: TeamLineupResult;
  away: TeamLineupResult;
}

export interface InjuredPlayer extends Player {
  injuryStatus: string;
}

export interface TeamInjuriesResult {
  injuries: InjuredPlayer[];
}

export interface MatchInjuriesResult {
  home: TeamInjuriesResult;
  away: TeamInjuriesResult;
}

/** Timeline event from GET api/matchinsights/matches/:id/events */
export interface MatchEventDto {
  playerName: string;
  clubId: number;
  eventTypeId: number;
  eventType: string;
  minute: number;
}

export interface RecentMatch {
  matchId: number;
  opponent: string;
  score: string;
  result: string;
  date: string;
}

export interface ClubLeagueStats {
  position: number;
  points: number;
  wins: number;
  draws: number;
  losses: number;
  goalsFor: number;
  goalsAgainst: number;
  xg: number;
  xgDiff: number;
  xga: number;
  xgaDiff: number;
  xpts: number;
  xptsDiff: number;
}

export interface TeamMetrics {
  name: string;
  totalWins: number;
  totalGoalsScored: number;
  totalGoalsConceded: number;
  homeWins: number;
  awayWins: number;
  winPercentage: number;
  avgGoalsScored: number;
  avgGoalsConceded: number;
}

export interface HeadToHead {
  summary: string;
  totalMatches: number;
  totalDraws: number;
  teamA: TeamMetrics;
  teamB: TeamMetrics;
}

export interface PricePoint {
  price: number;
  timestamp: string;
}

export interface OutcomePriceTimeline {
  outcomeName: string;
  timeline: PricePoint[];
}

export interface MarketPriceHistory {
  marketKey: string;
  marketDisplayName: string | null;
  outcomes: OutcomePriceTimeline[];
}

export interface PlayerRecentRatings {
  player: string;
  recentRatings: number[];
  avgRating: number;
}

export interface PlayerMatchRating {
  player: string;
  rating: number;
}

export interface TeamPerformanceMatchStats {
  matchId: number;
  opponent: string;
  date: string;
  teamRating: number | null;
  formation: string;
  playerRatings: PlayerMatchRating[];
}

export interface TeamPerformanceResult {
  topPlayers: PlayerRecentRatings[];
  recentTeamRatings: number[];
  avgTeamRating: number;
  formations: string[];
  matches: TeamPerformanceMatchStats[];
}

export interface ClubPair<T> {
  home: T;
  away: T;
}
