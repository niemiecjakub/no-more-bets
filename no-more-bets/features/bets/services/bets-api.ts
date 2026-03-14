import axiosInstance from "../../../lib/axios";
import type { BetSlipListItem } from "../interfaces";

/**
 * Fetches all bet slips from the backend (newest first).
 */
export async function fetchBetSlips(): Promise<BetSlipListItem[]> {
  const { data } = await axiosInstance.get<BetSlipListItem[]>(
    "/api/Database/bet-slips"
  );
  return data;
}
