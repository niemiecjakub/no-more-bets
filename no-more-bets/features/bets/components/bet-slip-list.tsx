import Link from "next/link";
import type { BetSelectionItem, BetSlipListItem } from "../interfaces";
import { BET_STATUS } from "../interfaces";
import { formatMatchDate } from "../../../utils/format-date";

interface BetSlipListProps {
  betSlips: BetSlipListItem[];
}

function getStatusBadgeClass(statusId: number): string {
  switch (statusId) {
    case BET_STATUS.Pending:
      return "bg-amber-100 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30";
    case BET_STATUS.Won:
      return "bg-emerald-100 text-emerald-800 ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30";
    case BET_STATUS.Lost:
      return "bg-red-100 text-red-800 ring-red-600/20 dark:bg-red-900/40 dark:text-red-400 dark:ring-red-500/30";
    case BET_STATUS.CashedOut:
      return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
    default:
      return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
  }
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pl-PL", {
    style: "currency",
    currency: "PLN",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function SelectionRow({ selection }: { selection: BetSelectionItem }) {
  return (
    <li className="border-t border-zinc-100 dark:border-zinc-800/80 first:border-t-0 py-2 first:pt-0 last:pb-0">
      <Link
        href={`/match/${selection.matchId}`}
        className="block rounded-md hover:bg-zinc-50 dark:hover:bg-zinc-900/50 -mx-1 px-1 py-0.5 transition-colors"
      >
        <div className="text-sm font-medium text-foreground">
          {selection.homeClubName}
          <span className="mx-1.5 text-zinc-500 dark:text-zinc-400">vs</span>
          {selection.awayClubName}
        </div>
        <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-zinc-600 dark:text-zinc-400">
          <span>{selection.eventTypeName}</span>
          <span className="font-medium text-foreground">{selection.eventOptionName}</span>
          <span className="tabular-nums">@{selection.oddsAtPlacement.toFixed(2)}</span>
        </div>
      </Link>
    </li>
  );
}

export function BetSlipList({ betSlips }: BetSlipListProps) {
  if (betSlips.length === 0) {
    return (
      <p className="py-12 text-center text-zinc-500 dark:text-zinc-400">
        No bet slips yet.
      </p>
    );
  }

  return (
    <ul className="space-y-4">
      {betSlips.map((slip) => (
        <li
          key={slip.id}
          className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 shadow-sm"
        >
          <div className="px-4 py-3 flex flex-wrap items-center justify-between gap-2 border-b border-zinc-100 dark:border-zinc-800/80">
            <div className="flex items-center gap-2 flex-wrap">
              <time
                dateTime={slip.createdAt}
                className="text-sm text-zinc-600 dark:text-zinc-400 tabular-nums"
              >
                {formatMatchDate(slip.createdAt)}
              </time>
              <span
                className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(slip.statusId)}`}
              >
                {slip.statusName}
              </span>
            </div>
          </div>
          <div className="px-4 py-3 grid grid-cols-1 sm:grid-cols-3 gap-3 text-sm border-b border-zinc-100 dark:border-zinc-800/80">
            <div>
              <span className="text-zinc-500 dark:text-zinc-400">Stake</span>
              <p className="font-semibold tabular-nums text-foreground">
                {formatCurrency(slip.stakeAmount)}
              </p>
            </div>
            <div>
              <span className="text-zinc-500 dark:text-zinc-400">Combined odds</span>
              <p className="font-semibold tabular-nums text-foreground">
                {slip.totalOdds.toFixed(2)}
              </p>
            </div>
            <div>
              <span className="text-zinc-500 dark:text-zinc-400">Potential payout</span>
              <p className="font-semibold tabular-nums text-foreground">
                {formatCurrency(slip.potentialPayout)}
              </p>
            </div>
          </div>
          <ul className="px-4 py-3">
            {slip.selections.map((sel, idx) => (
              <SelectionRow key={`${slip.id}-${sel.matchId}-${idx}`} selection={sel} />
            ))}
          </ul>
        </li>
      ))}
    </ul>
  );
}
