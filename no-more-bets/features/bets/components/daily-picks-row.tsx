"use client";

import type { BetSlipListItem } from "../interfaces";
import { BET_RISK_LEVEL } from "../interfaces";
import { BetSlipCard } from "./bet-slip-list";

const RISK_COLUMNS = [
  { id: BET_RISK_LEVEL.Low, label: "Low" },
  { id: BET_RISK_LEVEL.Medium, label: "Medium" },
  { id: BET_RISK_LEVEL.High, label: "High" },
] as const;

interface DailyPicksRowProps {
  slips: BetSlipListItem[];
}

export function DailyPicksRow({ slips }: DailyPicksRowProps) {
  return (
    <section className="mb-6">
      <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        Today&apos;s daily picks
      </h2>
      <ul className="grid grid-cols-1 gap-4 lg:grid-cols-3 lg:items-start">
        {RISK_COLUMNS.map((column) => {
          const slip = slips.find((item) => item.riskLevelId === column.id);
          return (
            <li key={column.id} className="min-w-0">
              <h3 className="mb-2 text-sm font-semibold text-foreground">{column.label}</h3>
              {slip ? (
                <ul>
                  <BetSlipCard slip={slip} showSessionLink />
                </ul>
              ) : (
                <div className="rounded-lg border border-dashed border-zinc-200 bg-white px-4 py-6 text-sm text-zinc-500 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                  No pick today.
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </section>
  );
}
