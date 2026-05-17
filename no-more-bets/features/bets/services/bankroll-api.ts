import axiosInstance from "@/lib/axios";
import type {
  BankrollBettingBalance,
  BankrollDashboard,
  BankrollEntriesPage,
  BankrollEntryBetDetailsDto,
} from "../interfaces";

const BANKROLL_ENTRIES_PAGE_SIZE = 25;

export interface FetchBankrollEntriesPageParams {
  limit?: number;
  afterCreatedAt?: string;
  afterId?: number;
}

export async function fetchBankrollDashboard(): Promise<BankrollDashboard> {
  const { data } = await axiosInstance.get<BankrollDashboard>(
    "/api/bankroll"
  );
  return data;
}

export async function fetchBankrollBettingBalance(): Promise<BankrollBettingBalance> {
  const { data } = await axiosInstance.get<BankrollBettingBalance>(
    "/api/bankroll/betting-balance"
  );
  return data;
}

export async function fetchBankrollEntriesPage(
  params: FetchBankrollEntriesPageParams = {},
): Promise<BankrollEntriesPage> {
  const { data } = await axiosInstance.get<BankrollEntriesPage>("/api/bankroll/entries", {
    params: {
      limit: params.limit ?? BANKROLL_ENTRIES_PAGE_SIZE,
      afterCreatedAt: params.afterCreatedAt,
      afterId: params.afterId,
    },
  });
  return data;
}

export async function fetchBankrollEntryBetDetails(entryId: number): Promise<BankrollEntryBetDetailsDto> {
  const { data } = await axiosInstance.get<BankrollEntryBetDetailsDto>(
    `/api/bankroll/entries/${entryId}/bet-details`
  );
  return data;
}
