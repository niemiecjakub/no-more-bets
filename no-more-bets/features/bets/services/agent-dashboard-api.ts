import axiosInstance from "@/lib/axios";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryDetails,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardPendingBetsWidget,
} from "../interfaces";

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

export async function fetchAgentDashboardPendingBetsWidget(): Promise<AgentDashboardPendingBetsWidget> {
  const { data } = await axiosInstance.get<AgentDashboardPendingBetsWidget>(
    "/api/agent/dashboard/pending-bets"
  );
  return data;
}
