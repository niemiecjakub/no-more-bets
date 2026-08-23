import { apiGetJson } from "@/lib/api-server";
import type { LeagueListItem, LeagueTable, SeasonYearItem } from "@/features/leagues/interfaces";

export async function getLeagues(): Promise<LeagueListItem[]> {
  const raw = await apiGetJson<LeagueListItem[]>("/api/leagues");
  return raw ?? [];
}

export async function getSeasonYears(): Promise<SeasonYearItem[]> {
  const raw = await apiGetJson<SeasonYearItem[]>("/api/seasons");
  return raw ?? [];
}

export async function getLeagueTable(
  leagueId: number,
  seasonId: number,
  clubId?: number,
): Promise<LeagueTable | null> {
  const params = new URLSearchParams({ seasonId: String(seasonId) });
  if (clubId != null) params.set("clubId", String(clubId));
  return apiGetJson<LeagueTable>(`/api/leagues/${leagueId}/table?${params.toString()}`);
}
