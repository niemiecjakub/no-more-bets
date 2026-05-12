import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import { fetchLeagues } from "@/features/leagues/services/leagues-api";
import type { LeagueListItem } from "@/features/leagues/interfaces";

interface LeagueStore {
  leagues: LeagueListItem[];
  isLoading: boolean;
  error: string | null;
  setLeagues: () => Promise<void>;
}

export const useLeagueStore = create<LeagueStore>((set) => ({
  leagues: [],
  isLoading: false,
  error: null,

  setLeagues: async () => {
    set({ isLoading: true, error: null });
    try {
      const leagues = await fetchLeagues();
      set({ leagues, error: null });
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load leagues."),
      });
    } finally {
      set({ isLoading: false });
    }
  },
}));
