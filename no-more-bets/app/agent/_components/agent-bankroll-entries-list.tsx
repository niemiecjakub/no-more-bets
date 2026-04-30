import type { BankrollEntryListItemDto } from "@/features/bets/interfaces";
import { formatCurrency } from "@/utils/format-currency";

interface AgentBankrollEntriesListProps {
  entries: BankrollEntryListItemDto[];
  selectedEntryId: number | null;
  onSelectEntry: (entry: BankrollEntryListItemDto) => void;
  isLoading: boolean;
}

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return date.toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
    hour12: false,
  });
}

export function AgentBankrollEntriesList({
  entries,
  selectedEntryId,
  onSelectEntry,
  isLoading,
}: AgentBankrollEntriesListProps) {
  if (isLoading) {
    return (
      <div className="h-full min-h-[min(78vh,44rem)] animate-pulse p-3">
        <div className="h-full rounded-lg bg-zinc-100 dark:bg-zinc-900" />
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <div className="p-4 text-sm text-zinc-500 dark:text-zinc-400">
        No bankroll entries found.
      </div>
    );
  }

  return (
    <div className="h-full max-h-[min(78vh,44rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600">
      <ul className="space-y-1 p-2">
        {entries.map((entry) => {
          const isActive = selectedEntryId === entry.id;
          const isIn = entry.flow === "In";
          return (
            <li key={entry.id} className="w-full min-w-0">
              <button
                type="button"
                onClick={() => onSelectEntry(entry)}
                className={`flex w-full min-w-0 flex-col gap-1.5 rounded-md border px-3 py-2.5 text-left transition-colors ${
                  isActive
                    ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                    : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80"
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <span className="line-clamp-2 text-sm font-medium text-foreground">{entry.name}</span>
                  <span className={`text-sm font-semibold tabular-nums ${isIn ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                    {isIn ? "+" : "-"}
                    {formatCurrency(entry.amount)}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2 text-xs text-zinc-500 dark:text-zinc-500">
                  <span>{formatDateTime(entry.createdAt)}</span>
                </div>
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
