import { apiGetJson } from "@/lib/api-server";
import type { BetSlipListItem } from "../interfaces";
import { normalizeBetSlip } from "./bets-api";

export async function getDailyPicks(): Promise<BetSlipListItem[]> {
  const raw = await apiGetJson<unknown[]>("/api/daily-picks");
  return (raw ?? []).map(normalizeBetSlip);
}
