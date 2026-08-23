import { apiGetJson } from "@/lib/api-server";
import type { ClubDetail, ClubListItem, ClubNextMatch } from "@/features/clubs/interfaces";
import type { MatchListItem, RecentMatch } from "@/features/matches/interfaces";
import { normalizeMatchListItem } from "@/features/matches/services/matches-api";

export async function getClubs(): Promise<ClubListItem[]> {
  const raw = await apiGetJson<ClubListItem[]>("/api/clubs");
  return raw ?? [];
}

export async function getClubById(clubId: number): Promise<ClubDetail | null> {
  return apiGetJson<ClubDetail>(`/api/clubs/${clubId}`);
}

export async function getClubBySlug(slug: string): Promise<ClubDetail | null> {
  const clubs = await getClubs();
  const match = clubs.find((club) => club.slug.toLowerCase() === slug.toLowerCase());
  return match ?? null;
}

export async function getClubNextMatch(clubId: number): Promise<ClubNextMatch | null> {
  return apiGetJson<ClubNextMatch>(`/api/clubs/${clubId}/next-match`);
}

export async function getClubRecentGames(clubId: number): Promise<RecentMatch[]> {
  const raw = await apiGetJson<RecentMatch[]>(`/api/clubs/${clubId}/recent-games`);
  return raw ?? [];
}

export async function getClubMatches(clubId: number): Promise<MatchListItem[]> {
  const raw = await apiGetJson<unknown[]>(`/api/clubs/${clubId}/matches`);
  if (!Array.isArray(raw)) return [];
  return raw.map(normalizeMatchListItem);
}
