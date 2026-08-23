import axios from "axios";
import axiosInstance from "../../../lib/axios";
import {
  normalizeBetStatus,
  type BetSlipSummaryDto,
  type MatchResearchBetSlipDto,
  type ResearchBetScenarioStatsDto,
} from "../../bets/interfaces";
import type {
  ClubLeagueStats,
  ClubPair,
  HeadToHead,
  MarketPriceHistory,
  MatchEventDto,
  MatchInjuriesResult,
  MatchLineupResult,
  MatchResearchOutput,
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

export async function fetchMatchEvents(matchId: number): Promise<MatchEventDto[]> {
  const { data } = await axiosInstance.get<MatchEventDto[]>(
    `/api/matchinsights/matches/${matchId}/events`
  );
  return data;
}

export function normalizeMatchResearchOutput(raw: unknown): MatchResearchOutput | null {
  if (raw == null || typeof raw !== "object") return null;
  const item = raw as Record<string, unknown>;
  const matchOverview =
    typeof item.matchOverview === "string"
      ? item.matchOverview
      : typeof item.MatchOverview === "string"
        ? item.MatchOverview
        : null;
  if (matchOverview == null) return null;

  const keyPointsRaw = item.keyPoints ?? item.KeyPoints;
  const risksRaw = item.risksAndUnknowns ?? item.RisksAndUnknowns;

  return {
    matchOverview,
    keyPoints: Array.isArray(keyPointsRaw)
      ? keyPointsRaw.filter((p): p is string => typeof p === "string")
      : [],
    risksAndUnknowns: Array.isArray(risksRaw)
      ? risksRaw.filter((r): r is string => typeof r === "string")
      : [],
  };
}

export function parseMatchResearchOutputText(text: string): MatchResearchOutput | null {
  const trimmed = text.trim();
  if (!trimmed.startsWith("{")) return null;

  try {
    return normalizeMatchResearchOutput(JSON.parse(trimmed));
  } catch {
    return null;
  }
}

export async function fetchMatchAgentResearch(matchId: number): Promise<MatchResearchOutput | null> {
  const { data } = await axiosInstance.get<unknown>(
    `/api/matchinsights/matches/${matchId}/agent-research`
  );
  return normalizeMatchResearchOutput(data);
}

type BetSlipSummaryApiDto = Omit<BetSlipSummaryDto, "status" | "selections"> & {
  status: unknown;
  selections: Array<Omit<BetSlipSummaryDto["selections"][number], "status"> & { status: unknown }>;
};

type MatchResearchBetSlipApiDto = {
  slip: BetSlipSummaryApiDto;
  scenarios: (Omit<ResearchBetScenarioStatsDto, "parlay" | "singles"> & {
    parlay: ResearchBetScenarioStatsDto["parlay"];
    singles: Omit<ResearchBetScenarioStatsDto["singles"], "legs"> & {
      legs: Array<Omit<ResearchBetScenarioStatsDto["singles"]["legs"][number], "status"> & { status: unknown }>;
    };
  }) | null;
};

function mapBetSlipSummaryFromApi(raw: BetSlipSummaryApiDto): BetSlipSummaryDto {
  return {
    ...raw,
    status: normalizeBetStatus(raw.status),
    selections: raw.selections.map((sel) => ({
      ...sel,
      status: normalizeBetStatus(sel.status),
    })),
  };
}

export function mapMatchResearchBetSlipFromApi(raw: MatchResearchBetSlipApiDto): MatchResearchBetSlipDto {
  return {
    slip: mapBetSlipSummaryFromApi(raw.slip),
    scenarios:
      raw.scenarios == null
        ? null
        : {
            unitStake: raw.scenarios.unitStake,
            parlay: raw.scenarios.parlay,
            singles: {
              stakeTotal: raw.scenarios.singles.stakeTotal,
              potentialPayout: raw.scenarios.singles.potentialPayout,
              profit: raw.scenarios.singles.profit,
              legs: raw.scenarios.singles.legs.map((leg) => ({
                ...leg,
                status: normalizeBetStatus(leg.status),
              })),
            },
          },
  };
}

/** Latest research-phase paper slip for the match, or null when none exists (404). */
export async function fetchMatchResearchBetSlip(matchId: number): Promise<MatchResearchBetSlipDto | null> {
  try {
    const { data } = await axiosInstance.get<MatchResearchBetSlipApiDto>(
      `/api/matchinsights/matches/${matchId}/research-bet-slip`
    );
    return mapMatchResearchBetSlipFromApi(data);
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
