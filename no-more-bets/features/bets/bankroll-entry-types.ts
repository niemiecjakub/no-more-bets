import { Banknote, RotateCcw, Ticket, TrendingUp } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface BankrollEntryTypeDefinition {
  name: string;
  label: string;
  icon: LucideIcon;
}

/** Aligned with backend BankrollEntryNames. */
export const BANKROLL_ENTRY_TYPES: BankrollEntryTypeDefinition[] = [
  { name: "Salary", label: "Salary", icon: Banknote },
  { name: "Bet win", label: "Bet win", icon: TrendingUp },
  { name: "Bet stake", label: "Bet stake", icon: Ticket },
  { name: "Bet cancellation refund", label: "Cancellation refund", icon: RotateCcw },
];
