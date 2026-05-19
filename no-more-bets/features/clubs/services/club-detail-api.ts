import axiosInstance from "../../../lib/axios";
import type { RecentMatch } from "@/features/matches/interfaces";
import type {
  ClubBetSelectionStats,
  ClubDetail,
  ClubNextMatch,
} from "../interfaces";

export async function fetchClubById(clubId: number): Promise<ClubDetail> {
  const { data } = await axiosInstance.get<ClubDetail>(`/api/clubs/${clubId}`);
  return data;
}

export async function fetchClubRecentGames(clubId: number): Promise<RecentMatch[]> {
  const { data } = await axiosInstance.get<RecentMatch[]>(
    `/api/clubs/${clubId}/recent-games`
  );
  return data;
}

export async function fetchClubNextMatch(clubId: number): Promise<ClubNextMatch | null> {
  const response = await axiosInstance.get<ClubNextMatch>(
    `/api/clubs/${clubId}/next-match`,
    { validateStatus: (status) => status === 200 || status === 204 }
  );
  if (response.status === 204) {
    return null;
  }
  return response.data;
}

export async function fetchClubBetSelectionStats(
  clubId: number
): Promise<ClubBetSelectionStats> {
  const { data } = await axiosInstance.get<ClubBetSelectionStats>(
    `/api/clubs/${clubId}/bet-selection-stats`
  );
  return data;
}
