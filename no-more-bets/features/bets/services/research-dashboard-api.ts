import axiosInstance from "@/lib/axios";
import type { AgentDashboardResearchBettingSummaryWidget } from "../interfaces";

export async function fetchAgentDashboardResearchBettingSummaryWidget(
  leagueIds?: number[],
  seasonYears?: string[],
): Promise<AgentDashboardResearchBettingSummaryWidget> {
  const normalizedLeagueIds = (leagueIds ?? []).filter((id) => Number.isInteger(id) && id > 0);
  const normalizedSeasonYears = (seasonYears ?? [])
    .map((year) => year.trim())
    .filter((year) => year.length > 0);
  const params = new URLSearchParams();
  for (const leagueId of normalizedLeagueIds) {
    params.append("leagueIds", String(leagueId));
  }
  for (const seasonYear of normalizedSeasonYears) {
    params.append("seasonYears", seasonYear);
  }
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/research-betting-summary?${params.toString()}`
    : "/api/agent/dashboard/research-betting-summary";

  const { data } = await axiosInstance.get<AgentDashboardResearchBettingSummaryWidget>(
    endpoint
  );
  return data;
}
