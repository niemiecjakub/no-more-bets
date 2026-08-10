import axiosInstance from "@/lib/axios";
import { type PagedResponse } from "@/lib/paged-response";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryDetails,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardMemoriesWidget,
  AgentDashboardPendingBetsWidget,
  AgentDashboardSessionsWidget,
  BetSlipListItem,
} from "../interfaces";

const BETTING_SUMMARY_SLIPS_PAGE_SIZE = 10;

function appendSeasonYears(params: URLSearchParams, seasonYears?: string[]) {
  for (const seasonYear of seasonYears ?? []) {
    const trimmed = seasonYear.trim();
    if (trimmed.length > 0) params.append("seasonYears", trimmed);
  }
}

export interface FetchAgentDashboardBettingSummarySlipsPageParams {
  limit?: number;
  afterCreatedAt?: string;
  afterId?: number;
  seasonYears?: string[];
}

export async function fetchAgentDashboardBankrollWidget(
  seasonYears?: string[],
): Promise<AgentDashboardBankrollWidget> {
  const params = new URLSearchParams();
  appendSeasonYears(params, seasonYears);
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/bankroll?${params.toString()}`
    : "/api/agent/dashboard/bankroll";
  const { data } = await axiosInstance.get<AgentDashboardBankrollWidget>(endpoint);
  return data;
}

export async function fetchAgentDashboardBettingSummaryWidget(
  seasonYears?: string[],
): Promise<AgentDashboardBettingSummaryWidget> {
  const params = new URLSearchParams();
  appendSeasonYears(params, seasonYears);
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/betting-summary?${params.toString()}`
    : "/api/agent/dashboard/betting-summary";
  const { data } = await axiosInstance.get<AgentDashboardBettingSummaryWidget>(endpoint);
  return data;
}

export async function fetchAgentDashboardBettingSummaryDetails(
  seasonYears?: string[],
): Promise<AgentDashboardBettingSummaryDetails> {
  const params = new URLSearchParams();
  appendSeasonYears(params, seasonYears);
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/betting-summary/details?${params.toString()}`
    : "/api/agent/dashboard/betting-summary/details";
  const { data } = await axiosInstance.get<AgentDashboardBettingSummaryDetails>(endpoint);
  return data;
}

export async function fetchAgentDashboardBettingSummarySlipsPage(
  params: FetchAgentDashboardBettingSummarySlipsPageParams = {},
): Promise<PagedResponse<BetSlipListItem>> {
  const query = new URLSearchParams();
  query.set("limit", String(params.limit ?? BETTING_SUMMARY_SLIPS_PAGE_SIZE));
  if (params.afterCreatedAt) query.set("afterCreatedAt", params.afterCreatedAt);
  if (params.afterId != null) query.set("afterId", String(params.afterId));
  appendSeasonYears(query, params.seasonYears);

  const { data } = await axiosInstance.get<PagedResponse<BetSlipListItem>>(
    `/api/agent/dashboard/betting-summary/slips?${query.toString()}`,
  );
  return data;
}

export async function fetchAgentDashboardPendingBetsWidget(
  seasonYears?: string[],
): Promise<AgentDashboardPendingBetsWidget> {
  const params = new URLSearchParams();
  appendSeasonYears(params, seasonYears);
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/pending-bets?${params.toString()}`
    : "/api/agent/dashboard/pending-bets";
  const { data } = await axiosInstance.get<AgentDashboardPendingBetsWidget>(endpoint);
  return data;
}

export async function fetchAgentDashboardSessionsWidget(
  seasonYears?: string[],
): Promise<AgentDashboardSessionsWidget> {
  const params = new URLSearchParams();
  appendSeasonYears(params, seasonYears);
  const endpoint = params.size > 0
    ? `/api/agent/dashboard/sessions?${params.toString()}`
    : "/api/agent/dashboard/sessions";
  const { data } = await axiosInstance.get<AgentDashboardSessionsWidget>(endpoint);
  return data;
}

export async function fetchAgentDashboardMemoriesWidget(): Promise<AgentDashboardMemoriesWidget> {
  const { data } = await axiosInstance.get<AgentDashboardMemoriesWidget>(
    "/api/agent/dashboard/memories"
  );
  return data;
}
