import axiosInstance from "../../../lib/axios";
import type { MatchAnalysisPageDto, MatchListItem } from "../interfaces";

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
  const { data } = await axiosInstance.get<MatchAnalysisPageDto>(
    `/api/Database/matches/${matchId}/analyses`
  );
  return data;
}
