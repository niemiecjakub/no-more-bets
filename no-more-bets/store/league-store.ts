import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import {
  fetchLeagues,
  fetchLeagueTable,
} from "@/features/leagues/services/leagues-api";
import type {
  LeagueListItem,
  LeagueTableDto,
} from "@/features/leagues/interfaces";

interface LeagueStore {
  leagues: LeagueListItem[];
  leagueTableById: Record<number, LeagueTableDto>;
  isLoading: boolean;
  error: string | null;
  setLeagues: () => Promise<void>;
  setLeagueTable: (leagueId: number) => Promise<void>;
}

export const useLeagueStore = create<LeagueStore>((set) => ({
  leagues: [],
  leagueTableById: {},
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

  setLeagueTable: async (leagueId: number) => {
    set({ isLoading: true, error: null });
    try {
      const data = await fetchLeagueTable(leagueId);
      set((state) => ({
        leagueTableById: {
          ...state.leagueTableById,
          [leagueId]: data,
        },
        error: null,
      }));
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load league table."),
      });
    } finally {
      set({ isLoading: false });
    }
  },
}));
