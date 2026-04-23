import axios from "axios";
import axiosInstance from "../../../lib/axios";
import type { BetSlipSummaryDto } from "../../bets/interfaces";
import type {
  ClubLeagueStats,
  ClubPair,
  HeadToHead,
  MarketPriceHistory,
  MatchInjuriesResult,
  MatchLineupResult,
  RecentMatch,
  TeamPerformanceResult,
} from "../interfaces";

export async function fetchMatchLineups(matchId: number): Promise<MatchLineupResult | null> {
  const { data } = await axiosInstance.get<MatchLineupResult | null>(
    `/api/matchinsights/matches/${matchId}/lineups`
  );
  return data;
}

export async function fetchMatchInjuries(matchId: number): Promise<MatchInjuriesResult | null> {
  const { data } = await axiosInstance.get<MatchInjuriesResult | null>(
    `/api/matchinsights/matches/${matchId}/injuries`
  );
  return data;
}

export async function fetchMatchAgentResearch(matchId: number): Promise<string | null> {
  const { data } = await axiosInstance.get<string | null>(
    `/api/matchinsights/matches/${matchId}/agent-research`
  );
  return data;
}

/** Latest research-phase paper slip for the match, or null when none exists (404). */
export async function fetchMatchResearchBetSlip(matchId: number): Promise<BetSlipSummaryDto | null> {
  try {
    const { data } = await axiosInstance.get<BetSlipSummaryDto>(
      `/api/matchinsights/matches/${matchId}/research-bet-slip`
    );
    return data;
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) {
      return null;
    }
    throw err;
  }
}

export async function fetchMatchRecentGames(matchId: number): Promise<ClubPair<RecentMatch[] | null>> {
  const { data } = await axiosInstance.get<ClubPair<RecentMatch[] | null>>(
    `/api/matchinsights/matches/${matchId}/recent-games`
  );
  return data;
}

export async function fetchMatchLeagueStatistics(
  matchId: number
): Promise<ClubPair<ClubLeagueStats | null>> {
  const { data } = await axiosInstance.get<ClubPair<ClubLeagueStats | null>>(
    `/api/matchinsights/matches/${matchId}/league-statistics`
  );
  return data;
}

export async function fetchMatchHeadToHead(matchId: number): Promise<HeadToHead | null> {
  const { data } = await axiosInstance.get<HeadToHead | null>(
    `/api/matchinsights/matches/${matchId}/head-to-head`
  );
  return data;
}

export async function fetchMatchBettingOddsHistory(
  matchId: number
): Promise<MarketPriceHistory[] | null> {
  const { data } = await axiosInstance.get<MarketPriceHistory[] | null>(
    `/api/matchinsights/matches/${matchId}/betting-odds-history`
  );
  return data;
}

export async function fetchMatchRollingPerformance(
  matchId: number
): Promise<ClubPair<TeamPerformanceResult | null>> {
  const { data } = await axiosInstance.get<ClubPair<TeamPerformanceResult | null>>(
    `/api/matchinsights/matches/${matchId}/rolling-performance`
  );
  return data;
}
