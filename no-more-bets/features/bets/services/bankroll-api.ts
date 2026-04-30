import axiosInstance from "@/lib/axios";
import type {
  BankrollBettingBalance,
  BankrollDashboard,
  BankrollEntryBetDetailsDto,
  BankrollEntryListItemDto,
  BankrollFlowPointDto,
} from "../interfaces";

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

export async function fetchBankrollFlowPoints(): Promise<BankrollFlowPointDto[]> {
  const { data } = await axiosInstance.get<BankrollFlowPointDto[]>(
    "/api/bankroll/flow-points"
  );
  return data;
}

export async function fetchBankrollEntries(): Promise<BankrollEntryListItemDto[]> {
  const { data } = await axiosInstance.get<BankrollEntryListItemDto[]>(
    "/api/bankroll/entries"
  );
  return data;
}

export async function fetchBankrollEntryBetDetails(entryId: number): Promise<BankrollEntryBetDetailsDto> {
  const { data } = await axiosInstance.get<BankrollEntryBetDetailsDto>(
    `/api/bankroll/entries/${entryId}/bet-details`
  );
  return data;
}
