import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import { fetchClubs } from "@/features/clubs/services/clubs-api";
import type { ClubListItem } from "@/features/clubs/interfaces";

interface ClubStore {
  clubs: ClubListItem[];
  isLoading: boolean;
  error: string | null;
  setClubs: () => Promise<void>;
}

export const useClubStore = create<ClubStore>((set) => ({
  clubs: [],
  isLoading: false,
  error: null,

  setClubs: async () => {
    set({ isLoading: true, error: null });
    try {
      const clubs = await fetchClubs();
      set({ clubs, error: null });
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load clubs."),
      });
    } finally {
      set({ isLoading: false });
    }
  },
}));
