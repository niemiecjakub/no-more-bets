import axiosInstance from "@/lib/axios";
import type { BankrollDashboard } from "../interfaces";

export async function fetchBankrollDashboard(): Promise<BankrollDashboard> {
  const { data } = await axiosInstance.get<BankrollDashboard>(
    "/api/Database/bankroll"
  );
  return data;
}
