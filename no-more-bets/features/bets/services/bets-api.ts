import axiosInstance from "../../../lib/axios";
import type { BetSlipListItem } from "../interfaces";

function normalizeBetSlip(raw: unknown): BetSlipListItem {
  const r = raw as Record<string, unknown>;
  const item = raw as BetSlipListItem;
  const agentSessionIdRaw =
    item.agentSessionId ?? r.agentSessionId ?? r.AgentSessionId;
  const agentSessionId =
    typeof agentSessionIdRaw === "number" && Number.isFinite(agentSessionIdRaw)
      ? agentSessionIdRaw
      : null;
  return { ...item, agentSessionId };
}

/**
 * Fetches all bet slips from the backend (newest first).
 */
export async function fetchBetSlips(): Promise<BetSlipListItem[]> {
  const { data } = await axiosInstance.get<unknown[]>(
    "/api/Database/bet-slips"
  );
  return data.map(normalizeBetSlip);
}
