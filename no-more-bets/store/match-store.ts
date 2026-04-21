import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import {
  fetchMatches,
  fetchMatchAnalysisPage,
  type FetchMatchesFilters,
} from "@/features/matches/services/matches-api";
import type { MatchListItem, MatchAnalysisPageDto } from "@/features/matches/interfaces";

interface MatchStore {
  matches: MatchListItem[];
  matchAnalysisById: Record<number, MatchAnalysisPageDto>;
  isLoading: boolean;
  error: string | null;
  setMatches: (filters?: FetchMatchesFilters) => Promise<void>;
  setMatchAnalysisPage: (matchId: number) => Promise<void>;
}

export const useMatchStore = create<MatchStore>((set) => ({
  matches: [],
  matchAnalysisById: {},
  isLoading: false,
  error: null,

  setMatches: async (filters) => {
    set({ isLoading: true, error: null });
    try {
      const matches = await fetchMatches(filters);
      set({ matches, error: null });
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load matches."),
      });
    } finally {
      set({ isLoading: false });
    }
  },

  setMatchAnalysisPage: async (matchId: number) => {
    set({ isLoading: true, error: null });
    try {
      const data = await fetchMatchAnalysisPage(matchId);
      set((state) => ({
        matchAnalysisById: {
          ...state.matchAnalysisById,
          [matchId]: data,
        },
        error: null,
      }));
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load match analyses."),
      });
    } finally {
      set({ isLoading: false });
    }
  },
}));
