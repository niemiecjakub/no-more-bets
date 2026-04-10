import type { BankrollDashboard, BankrollRecord } from "../interfaces";
import { formatMatchDate } from "@/utils/format-date";

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pl-PL", {
    style: "currency",
    currency: "PLN",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function paydayLabel(days: number): string {
  if (days === 0) return "Payday today";
  if (days === 1) return "1 day until payday";
  return `${days} days until payday`;
}

function BankrollRecordRow({ record }: { record: BankrollRecord }) {
  const isIn = record.flow === "In";
  return (
    <li className="border-b border-zinc-100 py-3 text-sm last:border-0 dark:border-zinc-800/80">
      <div className="flex items-start justify-between gap-2">
        <span className="font-medium text-foreground">{record.name}</span>
        <span
          className={
            isIn
              ? "shrink-0 tabular-nums font-medium text-emerald-600 dark:text-emerald-400"
              : "shrink-0 tabular-nums text-zinc-700 dark:text-zinc-300"
          }
        >
          {isIn ? "+" : "−"}
          {formatCurrency(record.amount)}
        </span>
      </div>
      <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-xs text-zinc-500 dark:text-zinc-400">
        <span>{formatMatchDate(record.createdAt)}</span>
        {record.betId != null && <span>Bet #{record.betId}</span>}
      </div>
    </li>
  );
}

export function BankrollSidebarSkeleton() {
  return (
    <div className="animate-pulse overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="space-y-3 border-b border-zinc-100 p-4 dark:border-zinc-800">
        <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-8 w-32 rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-3 w-40 rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      <div className="p-4">
        <div className="mb-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="border-b border-zinc-100 py-3 last:border-0 dark:border-zinc-800/80"
          >
            <div className="flex justify-between gap-2">
              <div className="h-4 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-4 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
            <div className="mt-2 h-3 w-40 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        ))}
      </div>
    </div>
  );
}

interface BankrollSidebarProps {
  data: BankrollDashboard | null;
  isLoading: boolean;
  error: string | null;
}

export function BankrollSidebar({ data, isLoading, error }: BankrollSidebarProps) {
  if (isLoading && !data) {
    return <BankrollSidebarSkeleton />;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {error}
      </div>
    );
  }

  if (!data) {
    return null;
  }

  return (
    <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="border-b border-zinc-100 p-4 dark:border-zinc-800">
        <h2 className="text-sm font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Bankroll
        </h2>
        <p className="mt-2 text-2xl font-semibold tabular-nums tracking-tight text-foreground">
          {formatCurrency(data.currentBalance)}
        </p>
        <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
          {paydayLabel(data.daysUntilPayday)}
        </p>
      </div>
      <div className="p-4">
        <h3 className="mb-2 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Ledger
        </h3>
        {data.records.length === 0 ? (
          <p className="text-sm text-zinc-500 dark:text-zinc-400">No entries yet.</p>
        ) : (
          <ul className="max-h-[min(24rem,50vh)] overflow-y-auto overscroll-contain -mx-1 px-1">
            {data.records.map((r) => (
              <BankrollRecordRow key={r.id} record={r} />
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
