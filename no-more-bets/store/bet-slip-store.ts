import { create } from "zustand";
import { handleServiceError } from "@/lib/error-handler";
import { fetchBetSlips } from "@/features/bets/services/bets-api";
import type { BetSlipListItem } from "@/features/bets/interfaces";

interface BetSlipStore {
  betSlips: BetSlipListItem[];
  isLoading: boolean;
  error: string | null;
  setBetSlips: () => Promise<void>;
}

export const useBetSlipStore = create<BetSlipStore>((set) => ({
  betSlips: [],
  isLoading: false,
  error: null,

  setBetSlips: async () => {
    set({ isLoading: true, error: null });
    try {
      const betSlips = await fetchBetSlips();
      set({ betSlips, error: null });
    } catch (err) {
      set({
        error: handleServiceError(err, "Failed to load bet slips."),
      });
    } finally {
      set({ isLoading: false });
    }
  },
}));
