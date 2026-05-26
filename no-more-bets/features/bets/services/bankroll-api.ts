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
  entryNames?: string[];
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
  const queryParams = new URLSearchParams();
  queryParams.set("limit", String(params.limit ?? BANKROLL_ENTRIES_PAGE_SIZE));

  if (params.afterCreatedAt != null) {
    queryParams.set("afterCreatedAt", params.afterCreatedAt);
  }
  if (params.afterId != null) {
    queryParams.set("afterId", String(params.afterId));
  }
  for (const entryName of params.entryNames ?? []) {
    if (entryName.trim().length > 0) {
      queryParams.append("entryNames", entryName);
    }
  }

  const { data } = await axiosInstance.get<PagedResponse<BankrollEntryListItemDto>>(
    `/api/bankroll/entries?${queryParams.toString()}`,
  );
  return data;
}

export async function fetchBankrollEntryBetDetails(entryId: number): Promise<BankrollEntryBetDetailsDto> {
  const { data } = await axiosInstance.get<BankrollEntryBetDetailsDto>(
    `/api/bankroll/entries/${entryId}/bet-details`
  );
  return data;
}
