"use client";

import type { BetSlipListItem } from "../interfaces";
import { BET_RISK_LEVEL } from "../interfaces";
import { cn } from "@/lib/utils";
import { BetSlipCard } from "./bet-slip-list";

const RISK_COLUMNS = [
  { id: BET_RISK_LEVEL.Low, label: "Low" },
  { id: BET_RISK_LEVEL.Medium, label: "Medium" },
  { id: BET_RISK_LEVEL.High, label: "High" },
] as const;

function riskBadgeClass(riskLevelId: number): string {
  switch (riskLevelId) {
    case BET_RISK_LEVEL.Low:
      return "bg-emerald-100 text-emerald-800 ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30";
    case BET_RISK_LEVEL.Medium:
      return "bg-amber-100 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30";
    case BET_RISK_LEVEL.High:
      return "bg-red-100 text-red-800 ring-red-600/20 dark:bg-red-900/40 dark:text-red-400 dark:ring-red-500/30";
    default:
      return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
  }
}

interface DailyPicksGridProps {
  slips: BetSlipListItem[];
  emptyLabel?: string;
}

export function DailyPicksGrid({ slips, emptyLabel = "No pick." }: DailyPicksGridProps) {
  return (
    <ul className="grid grid-cols-1 gap-4 lg:grid-cols-3 lg:items-start">
      {RISK_COLUMNS.map((column) => {
        const slip = slips.find((item) => item.riskLevelId === column.id);
        return (
          <li key={column.id} className="min-w-0">
            <h3
              className={cn(
                "mb-2 inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset",
                riskBadgeClass(column.id),
              )}
            >
              {column.label}
            </h3>
            {slip ? (
              <ul>
                <BetSlipCard slip={slip} showSessionLink />
              </ul>
            ) : (
              <div className="rounded-lg border border-dashed border-zinc-200 bg-white px-4 py-6 text-sm text-zinc-500 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                {emptyLabel}
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}

interface DailyPicksRowProps {
  slips: BetSlipListItem[];
}

export function DailyPicksRow({ slips }: DailyPicksRowProps) {
  return (
    <section className="mb-6">
      <DailyPicksGrid slips={slips} emptyLabel="No pick today." />
    </section>
  );
}
