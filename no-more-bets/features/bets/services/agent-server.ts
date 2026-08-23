import { apiGetJson } from "@/lib/api-server";
import type { AgentDashboardBankrollWidget } from "@/features/bets/interfaces";

export async function getAgentBankrollWidget(): Promise<AgentDashboardBankrollWidget | null> {
  return apiGetJson<AgentDashboardBankrollWidget>("/api/agent/dashboard/bankroll", {
    revalidate: 30,
  });
}
