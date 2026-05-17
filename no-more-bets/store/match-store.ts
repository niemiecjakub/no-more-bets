import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import {
  fetchMatchesPage,
  fetchMatchAnalysisPage,
  type FetchMatchesFilters,
} from "@/features/matches/services/matches-api";
import type { MatchListItem, MatchAnalysisPageDto } from "@/features/matches/interfaces";

function mergeMatches(existing: MatchListItem[], incoming: MatchListItem[]): MatchListItem[] {
  const seen = new Set(existing.map((match) => match.id));
  const merged = [...existing];
  for (const match of incoming) {
    if (seen.has(match.id)) continue;
    seen.add(match.id);
    merged.push(match);
  }
  return merged;
}

interface MatchStore {
  matches: MatchListItem[];
  matchAnalysisById: Record<number, MatchAnalysisPageDto>;
  isLoading: boolean;
  isLoadingMore: boolean;
  hasMore: boolean;
  nextCursor: { at: string; id: number } | null;
  lastFilters: FetchMatchesFilters | undefined;
  error: string | null;
  loadMoreError: string | null;
  setMatches: (filters?: FetchMatchesFilters) => Promise<void>;
  loadMoreMatches: () => Promise<void>;
  retryLoadMore: () => void;
  setMatchAnalysisPage: (matchId: number) => Promise<void>;
}

let isLoadingMoreInFlight = false;

export const useMatchStore = create<MatchStore>((set, get) => ({
  matches: [],
  matchAnalysisById: {},
  isLoading: false,
  isLoadingMore: false,
  hasMore: false,
  nextCursor: null,
  lastFilters: undefined,
  error: null,
  loadMoreError: null,

  setMatches: async (filters) => {
    set({
      isLoading: true,
      error: null,
      loadMoreError: null,
      hasMore: false,
      nextCursor: null,
      lastFilters: filters,
    });
    try {
      const page = await fetchMatchesPage(filters);
      set({
        matches: page.items,
        hasMore: page.hasMore,
        nextCursor:
          page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
            ? { at: page.nextCursorAt, id: page.nextCursorId }
            : null,
        error: null,
      });
    } catch (err) {
      set({
        matches: [],
        error: handleServiceError(err, "Failed to load matches."),
      });
    } finally {
      set({ isLoading: false });
    }
  },

  loadMoreMatches: async () => {
    const { hasMore, nextCursor, lastFilters } = get();
    if (!hasMore || !nextCursor || isLoadingMoreInFlight) return;

    isLoadingMoreInFlight = true;
    set({ isLoadingMore: true, loadMoreError: null });
    try {
      const page = await fetchMatchesPage(lastFilters, {
        afterMatchDate: nextCursor.at,
        afterId: nextCursor.id,
      });
      set((state) => ({
        matches: mergeMatches(state.matches, page.items),
        hasMore: page.hasMore,
        nextCursor:
          page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
            ? { at: page.nextCursorAt, id: page.nextCursorId }
            : null,
      }));
    } catch (err) {
      set({
        loadMoreError: handleServiceError(err, "Failed to load more matches."),
      });
    } finally {
      isLoadingMoreInFlight = false;
      set({ isLoadingMore: false });
    }
  },

  retryLoadMore: () => {
    set({ loadMoreError: null });
    void get().loadMoreMatches();
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
