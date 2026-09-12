import { apiGetJson } from "@/lib/api-server";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import type { MatchAnalysisPageDto, MatchListItem, MatchResearchOutput } from "@/features/matches/interfaces";
import type { MatchResearchBetSlipDto } from "@/features/bets/interfaces";
import {
  mapMatchResearchBetSlipFromApi,
  normalizeMatchResearchOutput,
} from "@/features/matches/services/match-insights-api";
import {
  buildMatchesQuery,
  MATCH_DATE_SORT,
  normalizeMatchAnalysisPage,
  normalizeMatchListItem,
  type FetchMatchesFilters,
  type FetchMatchesPageParams,
  type MatchDateSortOrder,
} from "@/features/matches/services/matches-api";
import { MATCH_STATUS } from "@/features/matches/interfaces";

export async function getMatchesPage(
  filters?: FetchMatchesFilters,
  params: FetchMatchesPageParams = {},
): Promise<PagedResponse<MatchListItem>> {
  const raw = await apiGetJson<unknown>(`/api/matches?${buildMatchesQuery(filters, params)}`);
  return normalizePagedResponse(raw ?? { items: [] }, normalizeMatchListItem);
}

export async function getMatchAnalysisPage(matchId: number): Promise<MatchAnalysisPageDto | null> {
  const raw = await apiGetJson<unknown>(`/api/matches/${matchId}/analyses`);
  if (raw == null) return null;
  return normalizeMatchAnalysisPage(raw);
}

export async function getMatchAgentResearch(matchId: number): Promise<MatchResearchOutput | null> {
  const raw = await apiGetJson<unknown>(`/api/matchinsights/matches/${matchId}/agent-research`);
  return normalizeMatchResearchOutput(raw);
}

export async function getMatchResearchBetSlip(matchId: number): Promise<MatchResearchBetSlipDto | null> {
  const raw = await apiGetJson<unknown>(`/api/matchinsights/matches/${matchId}/research-bet-slip`);
  if (raw == null) return null;
  return mapMatchResearchBetSlipFromApi(raw as Parameters<typeof mapMatchResearchBetSlipFromApi>[0]);
}

export async function getUpcomingResearchedMatches(): Promise<MatchListItem[]> {
  const raw = await apiGetJson<unknown[]>("/api/matches/upcoming-researched");
  if (!Array.isArray(raw)) return [];
  return raw.map(normalizeMatchListItem);
}

export function defaultUpcomingSort(): MatchDateSortOrder {
  return MATCH_DATE_SORT.Ascending;
}

export function isFinishedMatch(statusId: number, homeGoals: number | null, awayGoals: number | null): boolean {
  return statusId === MATCH_STATUS.Finished && homeGoals != null && awayGoals != null;
}

export function isIndexableMatch(input: {
  hasResearch?: boolean;
  matchStatusId: number;
  homeGoals: number | null;
  awayGoals: number | null;
}): boolean {
  return Boolean(input.hasResearch) || isFinishedMatch(input.matchStatusId, input.homeGoals, input.awayGoals);
}
