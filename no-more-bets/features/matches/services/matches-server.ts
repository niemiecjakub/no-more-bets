import { apiGetJson } from "@/lib/api-server";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import type { MatchAnalysisPageDto, MatchListItem, MatchResearchOutput } from "@/features/matches/interfaces";
import type { MatchResearchBetSlipDto } from "@/features/bets/interfaces";
import {
  mapMatchResearchBetSlipFromApi,
  normalizeMatchResearchOutput,
} from "@/features/matches/services/match-insights-api";
import {
  MATCH_DATE_SORT,
  normalizeMatchAnalysisPage,
  normalizeMatchListItem,
  type FetchMatchesFilters,
  type FetchMatchesPageParams,
  type MatchDateSortOrder,
} from "@/features/matches/services/matches-api";
import { MATCH_STATUS } from "@/features/matches/interfaces";

const MATCHES_PAGE_SIZE = 10;

function matchesQuery(
  filters: FetchMatchesFilters | undefined,
  params: FetchMatchesPageParams,
): string {
  const queryParams = new URLSearchParams();
  queryParams.set("limit", String(params.limit ?? MATCHES_PAGE_SIZE));

  if (filters?.matchStatusId != null) {
    queryParams.set("matchStatusId", String(filters.matchStatusId));
  }
  if (filters?.sortOrder != null) {
    queryParams.set("sortOrder", filters.sortOrder);
  }
  const search = filters?.search?.trim();
  if (search) {
    queryParams.set("search", search);
  }
  for (const seasonYear of filters?.seasonYears ?? []) {
    const trimmed = seasonYear.trim();
    if (trimmed) queryParams.append("seasonYears", trimmed);
  }
  for (const leagueId of filters?.leagueIds ?? []) {
    if (Number.isInteger(leagueId) && leagueId > 0) {
      queryParams.append("leagueIds", String(leagueId));
    }
  }
  if (params.afterMatchDate != null) {
    queryParams.set("afterMatchDate", params.afterMatchDate);
  }
  if (params.afterId != null) {
    queryParams.set("afterId", String(params.afterId));
  }
  return queryParams.toString();
}

export async function getMatchesPage(
  filters?: FetchMatchesFilters,
  params: FetchMatchesPageParams = {},
): Promise<PagedResponse<MatchListItem>> {
  const raw = await apiGetJson<unknown>(`/api/matches?${matchesQuery(filters, params)}`);
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
