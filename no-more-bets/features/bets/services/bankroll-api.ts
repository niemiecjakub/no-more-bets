import axiosInstance from "@/lib/axios";
import { type PagedResponse } from "@/lib/paged-response";
import type {
  BankrollBettingBalance,
  BankrollDashboard,
  BankrollEntryBetDetailsDto,
  BankrollEntryListItemDto,
} from "../interfaces";

const BANKROLL_ENTRIES_PAGE_SIZE = 15;

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
): Promise<PagedResponse<BankrollEntryListItemDto>> {
  const { data } = await axiosInstance.get<PagedResponse<BankrollEntryListItemDto>>("/api/bankroll/entries", {
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
