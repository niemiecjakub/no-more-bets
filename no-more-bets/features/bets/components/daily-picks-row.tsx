"use client";

import type { BetSlipListItem } from "../interfaces";
import { BET_RISK_LEVEL } from "../interfaces";
import { BetSlipCard } from "./bet-slip-list";

const RISK_COLUMNS = [
  { id: BET_RISK_LEVEL.Low, label: "Low" },
  { id: BET_RISK_LEVEL.Medium, label: "Medium" },
  { id: BET_RISK_LEVEL.High, label: "High" },
] as const;

interface DailyPicksGridProps {
  slips: BetSlipListItem[];
  emptyLabel?: string;
}

export function DailyPicksGrid({ slips, emptyLabel = "No pick." }: DailyPicksGridProps) {
  return (
    <div className="-mx-4 overflow-x-auto px-4 [-ms-overflow-style:none] [scrollbar-width:none] sm:-mx-6 sm:px-6 lg:mx-0 lg:overflow-visible lg:px-0 [&::-webkit-scrollbar]:hidden">
      <ul className="grid auto-cols-[66%] grid-flow-col gap-4 snap-x snap-mandatory [&>li]:snap-start md:auto-cols-[38%] lg:grid-flow-row lg:auto-cols-auto lg:grid-cols-3 lg:items-start lg:snap-none">
        {RISK_COLUMNS.map((column) => {
          const slip = slips.find((item) => item.riskLevelId === column.id);
          return (
            <li key={column.id} className="min-w-0">
              {slip ? (
                <ul>
                  <BetSlipCard slip={slip} compact />
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
    </div>
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
