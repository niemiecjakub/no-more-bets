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

export interface FetchAgentDashboardBettingSummarySlipsPageParams {
  limit?: number;
  afterCreatedAt?: string;
  afterId?: number;
}

export async function fetchAgentDashboardBankrollWidget(): Promise<AgentDashboardBankrollWidget> {
  const { data } = await axiosInstance.get<AgentDashboardBankrollWidget>(
    "/api/agent/dashboard/bankroll"
  );
  return data;
}

export async function fetchAgentDashboardBettingSummaryWidget(): Promise<AgentDashboardBettingSummaryWidget> {
  const { data } = await axiosInstance.get<AgentDashboardBettingSummaryWidget>(
    "/api/agent/dashboard/betting-summary"
  );
  return data;
}

export async function fetchAgentDashboardBettingSummaryDetails(): Promise<AgentDashboardBettingSummaryDetails> {
  const { data } = await axiosInstance.get<AgentDashboardBettingSummaryDetails>(
    "/api/agent/dashboard/betting-summary/details"
  );
  return data;
}

export async function fetchAgentDashboardBettingSummarySlipsPage(
  params: FetchAgentDashboardBettingSummarySlipsPageParams = {},
): Promise<PagedResponse<BetSlipListItem>> {
  const { data } = await axiosInstance.get<PagedResponse<BetSlipListItem>>(
    "/api/agent/dashboard/betting-summary/slips",
    {
      params: {
        limit: params.limit ?? BETTING_SUMMARY_SLIPS_PAGE_SIZE,
        afterCreatedAt: params.afterCreatedAt,
        afterId: params.afterId,
      },
    },
  );
  return data;
}

export async function fetchAgentDashboardPendingBetsWidget(): Promise<AgentDashboardPendingBetsWidget> {
  const { data } = await axiosInstance.get<AgentDashboardPendingBetsWidget>(
    "/api/agent/dashboard/pending-bets"
  );
  return data;
}

export async function fetchAgentDashboardSessionsWidget(): Promise<AgentDashboardSessionsWidget> {
  const { data } = await axiosInstance.get<AgentDashboardSessionsWidget>(
    "/api/agent/dashboard/sessions"
  );
  return data;
}

export async function fetchAgentDashboardMemoriesWidget(): Promise<AgentDashboardMemoriesWidget> {
  const { data } = await axiosInstance.get<AgentDashboardMemoriesWidget>(
    "/api/agent/dashboard/memories"
  );
  return data;
}
