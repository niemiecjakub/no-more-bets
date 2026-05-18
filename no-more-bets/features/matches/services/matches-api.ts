import axiosInstance from "../../../lib/axios";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";
import {
  MATCH_STATUS,
  type MatchAnalysisPageDto,
  type MatchDetailsSummary,
  type MatchListItem,
} from "../interfaces";

export interface FetchMatchesFilters {
  matchStatusId?: number;
  leagueIds?: number[];
}

function optionalInt(v: unknown): number | null {
  if (typeof v === "number" && Number.isFinite(v)) return v;
  return null;
}

function optionalString(v: unknown): string | null {
  if (typeof v === "string") return v;
  return null;
}

function normalizeMatchDetails(raw: unknown): MatchDetailsSummary | null {
  if (!raw || typeof raw !== "object") return null;
  const details = raw as Record<string, unknown>;
  return {
    fotmobUrl: optionalString(details.fotmobUrl) ?? optionalString(details.FotmobUrl),
    fotmobDetailsJson:
      optionalString(details.fotmobDetailsJson) ?? optionalString(details.FotmobDetailsJson),
    fotmobReview: optionalString(details.fotmobReview) ?? optionalString(details.FotmobReview),
  };
}

function normalizeMatchAnalysisPage(raw: unknown): MatchAnalysisPageDto {
  const r = raw as Record<string, unknown>;
  const item = raw as MatchAnalysisPageDto;
  const homeSlug =
    (typeof item.homeClubSlug === "string" ? item.homeClubSlug : undefined) ??
    (typeof r.homeClubSlug === "string" ? r.homeClubSlug : undefined) ??
    (typeof r.HomeClubSlug === "string" ? r.HomeClubSlug : undefined) ??
    "";
  const awaySlug =
    (typeof item.awayClubSlug === "string" ? item.awayClubSlug : undefined) ??
    (typeof r.awayClubSlug === "string" ? r.awayClubSlug : undefined) ??
    (typeof r.AwayClubSlug === "string" ? r.AwayClubSlug : undefined) ??
    "";
  const matchStatusId =
    optionalInt(item.matchStatusId) ??
    optionalInt(r.matchStatusId) ??
    optionalInt(r.MatchStatusId) ??
    MATCH_STATUS.Upcoming;
  const homeGoals =
    optionalInt(item.homeGoals) ?? optionalInt(r.homeGoals) ?? optionalInt(r.HomeGoals);
  const awayGoals =
    optionalInt(item.awayGoals) ?? optionalInt(r.awayGoals) ?? optionalInt(r.AwayGoals);
  const researchAgentSessionId =
    optionalInt(item.researchAgentSessionId) ??
    optionalInt(r.researchAgentSessionId) ??
    optionalInt(r.ResearchAgentSessionId);
  const matchDetails =
    normalizeMatchDetails(item.matchDetails) ??
    normalizeMatchDetails(r.matchDetails) ??
    normalizeMatchDetails(r.MatchDetails);
  return {
    ...item,
    homeClubSlug: homeSlug,
    awayClubSlug: awaySlug,
    matchStatusId,
    homeGoals,
    awayGoals,
    researchAgentSessionId: researchAgentSessionId ?? null,
    matchDetails,
  };
}

function normalizeMatchListItem(raw: unknown): MatchListItem {
  const r = raw as Record<string, unknown>;
  const item = raw as MatchListItem;
  const homeSlug =
    (typeof item.homeClubSlug === "string" ? item.homeClubSlug : undefined) ??
    (typeof r.homeClubSlug === "string" ? r.homeClubSlug : undefined) ??
    (typeof r.HomeClubSlug === "string" ? r.HomeClubSlug : undefined) ??
    "";
  const awaySlug =
    (typeof item.awayClubSlug === "string" ? item.awayClubSlug : undefined) ??
    (typeof r.awayClubSlug === "string" ? r.awayClubSlug : undefined) ??
    (typeof r.AwayClubSlug === "string" ? r.AwayClubSlug : undefined) ??
    "";
  const leagueName = (
    (typeof item.leagueName === "string" ? item.leagueName : undefined) ??
    (typeof r.leagueName === "string" ? r.leagueName : undefined) ??
    (typeof r.LeagueName === "string" ? r.LeagueName : undefined) ??
    ""
  ).trim();
  const leagueSlug = (
    (typeof item.leagueSlug === "string" ? item.leagueSlug : undefined) ??
    (typeof r.leagueSlug === "string" ? r.leagueSlug : undefined) ??
    (typeof r.LeagueSlug === "string" ? r.LeagueSlug : undefined) ??
    ""
  ).trim();
  const hasResearch =
    (typeof item.hasResearch === "boolean" ? item.hasResearch : undefined) ??
    (typeof r.hasResearch === "boolean" ? r.hasResearch : undefined) ??
    (typeof r.HasResearch === "boolean" ? r.HasResearch : undefined) ??
    false;
  const hasResearchBet =
    (typeof item.hasResearchBet === "boolean" ? item.hasResearchBet : undefined) ??
    (typeof r.hasResearchBet === "boolean" ? r.hasResearchBet : undefined) ??
    (typeof r.HasResearchBet === "boolean" ? r.HasResearchBet : undefined) ??
    false;
  const hasLineup =
    (typeof item.hasLineup === "boolean" ? item.hasLineup : undefined) ??
    (typeof r.hasLineup === "boolean" ? r.hasLineup : undefined) ??
    (typeof r.HasLineup === "boolean" ? r.HasLineup : undefined) ??
    false;
  const hasOdds =
    (typeof item.hasOdds === "boolean" ? item.hasOdds : undefined) ??
    (typeof r.hasOdds === "boolean" ? r.hasOdds : undefined) ??
    (typeof r.HasOdds === "boolean" ? r.HasOdds : undefined) ??
    false;
  const hasHeadToHead =
    (typeof item.hasHeadToHead === "boolean" ? item.hasHeadToHead : undefined) ??
    (typeof r.hasHeadToHead === "boolean" ? r.hasHeadToHead : undefined) ??
    (typeof r.HasHeadToHead === "boolean" ? r.HasHeadToHead : undefined) ??
    false;

  return {
    ...item,
    homeClubSlug: homeSlug,
    awayClubSlug: awaySlug,
    leagueName,
    leagueSlug,
    hasResearch,
    hasResearchBet,
    hasLineup,
    hasOdds,
    hasHeadToHead,
  };
}

const MATCHES_PAGE_SIZE = 10;

export interface FetchMatchesPageParams {
  limit?: number;
  afterMatchDate?: string;
  afterId?: number;
}

/**
 * Fetches a page of matches from the backend (newest match date first).
 */
export async function fetchMatchesPage(
  filters?: FetchMatchesFilters,
  params: FetchMatchesPageParams = {},
): Promise<PagedResponse<MatchListItem>> {
  const queryParams = new URLSearchParams();
  queryParams.set("limit", String(params.limit ?? MATCHES_PAGE_SIZE));

  if (filters?.matchStatusId != null) {
    queryParams.set("matchStatusId", String(filters.matchStatusId));
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

  const { data } = await axiosInstance.get<unknown>(
    `/api/matches?${queryParams.toString()}`
  );
  return normalizePagedResponse(data, normalizeMatchListItem);
}

/**
 * Fetches match header and all analyses for a match.
 */
export async function fetchMatchAnalysisPage(
  matchId: number
): Promise<MatchAnalysisPageDto> {
  const { data } = await axiosInstance.get<unknown>(
    `/api/matches/${matchId}/analyses`
  );
  return normalizeMatchAnalysisPage(data);
}
