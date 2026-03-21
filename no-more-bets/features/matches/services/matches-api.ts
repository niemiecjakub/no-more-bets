import axiosInstance from "../../../lib/axios";
import { MATCH_STATUS, type MatchAnalysisPageDto, type MatchListItem } from "../interfaces";

function optionalInt(v: unknown): number | null {
  if (typeof v === "number" && Number.isFinite(v)) return v;
  return null;
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
  return {
    ...item,
    homeClubSlug: homeSlug,
    awayClubSlug: awaySlug,
    matchStatusId,
    homeGoals,
    awayGoals,
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
  return { ...item, homeClubSlug: homeSlug, awayClubSlug: awaySlug };
}

/**
 * Fetches all matches from the backend.
 */
export async function fetchMatches(): Promise<MatchListItem[]> {
  const { data } = await axiosInstance.get<unknown[]>(
    "/api/Database/matches"
  );
  return data.map(normalizeMatchListItem);
}

/**
 * Fetches match header and all analyses for a match.
 */
export async function fetchMatchAnalysisPage(
  matchId: number
): Promise<MatchAnalysisPageDto> {
  const { data } = await axiosInstance.get<unknown>(
    `/api/Database/matches/${matchId}/analyses`
  );
  return normalizeMatchAnalysisPage(data);
}
