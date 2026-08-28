import { apiGetJson } from "@/lib/api-server";
import type { BetSlipListItem } from "../interfaces";
import { normalizeBetSlip } from "./bets-api";
import { normalizePagedResponse, type PagedResponse } from "@/lib/paged-response";

export async function getDailyPicks(): Promise<BetSlipListItem[]> {
  const raw = await apiGetJson<unknown[]>("/api/daily-picks");
  return (raw ?? []).map(normalizeBetSlip);
}

export async function getDailyPicksPage(params?: {
  limit?: number;
  afterDate?: string;
}): Promise<PagedResponse<BetSlipListItem>> {
  const query = new URLSearchParams();
  query.set("limit", String(params?.limit ?? 7));
  if (params?.afterDate) query.set("afterDate", params.afterDate);
  const raw = await apiGetJson<unknown>(`/api/daily-picks/pages?${query.toString()}`);
  if (raw == null) {
    return { items: [], hasMore: false, nextCursorAt: null, nextCursorId: null };
  }
  return normalizePagedResponse(raw, normalizeBetSlip);
}
