import axiosInstance from "../../../lib/axios";
import type { BetSelectionItem, BetSlipListItem } from "../interfaces";

function normalizeSelection(raw: unknown): BetSelectionItem {
  const r = raw as Record<string, unknown>;
  const item = raw as BetSelectionItem;
  const homeClubSlug = item.homeClubSlug ?? r.homeClubSlug ?? r.HomeClubSlug;
  const awayClubSlug = item.awayClubSlug ?? r.awayClubSlug ?? r.AwayClubSlug;
  return {
    ...item,
    homeClubSlug:
      typeof homeClubSlug === "string" && homeClubSlug.trim() !== ""
        ? homeClubSlug
        : null,
    awayClubSlug:
      typeof awayClubSlug === "string" && awayClubSlug.trim() !== ""
        ? awayClubSlug
        : null,
  };
}

function normalizeBetSlip(raw: unknown): BetSlipListItem {
  const r = raw as Record<string, unknown>;
  const item = raw as BetSlipListItem;
  const agentSessionIdRaw =
    item.agentSessionId ?? r.agentSessionId ?? r.AgentSessionId;
  const agentSessionId =
    typeof agentSessionIdRaw === "number" && Number.isFinite(agentSessionIdRaw)
      ? agentSessionIdRaw
      : null;
  const selectionsRaw = r.selections ?? r.Selections;
  const selections = Array.isArray(selectionsRaw)
    ? selectionsRaw.map(normalizeSelection)
    : (item.selections ?? []);
  return { ...item, agentSessionId, selections };
}

/**
 * Fetches all bet slips from the backend (newest first).
 */
export async function fetchBetSlips(): Promise<BetSlipListItem[]> {
  const { data } = await axiosInstance.get<unknown[]>(
    "/api/bet-slips"
  );
  return data.map(normalizeBetSlip);
}
