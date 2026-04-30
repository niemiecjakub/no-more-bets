import axiosInstance from "@/lib/axios";
import type { AgentDashboardResearchBettingSummaryWidget } from "../interfaces";

export async function fetchAgentDashboardResearchBettingSummaryWidget(
  leagueIds?: number[]
): Promise<AgentDashboardResearchBettingSummaryWidget> {
  const normalizedLeagueIds = (leagueIds ?? []).filter((id) => Number.isInteger(id) && id > 0);
  const params = new URLSearchParams();
  for (const leagueId of normalizedLeagueIds) {
    params.append("leagueIds", String(leagueId));
  }
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/research-betting-summary?${params.toString()}`
    : "/api/agent/dashboard/research-betting-summary";

  const { data } = await axiosInstance.get<AgentDashboardResearchBettingSummaryWidget>(
    endpoint
  );
  return data;
}
