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
  return date.toLocaleString();
}

export function AgentBankrollEntriesList({
  entries,
  selectedEntryId,
  onSelectEntry,
  isLoading,
}: AgentBankrollEntriesListProps) {
  if (isLoading) {
    return <div className="h-full min-h-[420px] animate-pulse bg-zinc-100 dark:bg-zinc-900" />;
  }

  if (entries.length === 0) {
    return (
      <div className="p-4 text-sm text-zinc-500 dark:text-zinc-400">
        No bankroll entries found.
      </div>
    );
  }

  return (
    <div className="h-full max-h-[420px] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:theme(colors.zinc.400)_transparent] dark:[scrollbar-color:theme(colors.zinc.600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600">
      <ul className="divide-y divide-zinc-100 dark:divide-zinc-800">
        {entries.map((entry) => {
          const isActive = selectedEntryId === entry.id;
          const isIn = entry.flow === "In";
          return (
            <li key={entry.id}>
              <button
                type="button"
                onClick={() => onSelectEntry(entry)}
                className={`flex w-full flex-col gap-1 px-3 py-2 text-left transition-colors ${
                  isActive
                    ? "bg-zinc-100 dark:bg-zinc-900"
                    : "hover:bg-zinc-50 dark:hover:bg-zinc-900/70"
                }`}
              >
                <div className="flex items-start justify-between gap-2">
                  <span className="text-sm font-medium text-foreground">{entry.name}</span>
                  <span className={`text-sm font-semibold tabular-nums ${isIn ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400"}`}>
                    {isIn ? "+" : "-"}
                    {formatCurrency(entry.amount)}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2 text-xs text-zinc-500 dark:text-zinc-400">
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
