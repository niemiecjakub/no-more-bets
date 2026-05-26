"use client";

import { useEffect, useRef } from "react";
import type { BankrollEntryListItemDto } from "@/features/bets/interfaces";
import { formatCurrency } from "@/utils/format-currency";

interface AgentBankrollEntriesListProps {
  entries: BankrollEntryListItemDto[];
  selectedEntryId: number | null;
  onSelectEntry: (entry: BankrollEntryListItemDto) => void;
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
  loadMoreError: string | null;
  onRetryLoadMore: () => void;
  emptyMessage?: string;
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
  hasMore,
  isLoadingMore,
  onLoadMore,
  loadMoreError,
  onRetryLoadMore,
  emptyMessage = "No bankroll entries found.",
}: AgentBankrollEntriesListProps) {
  const scrollRootRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isLoading || isLoadingMore || !hasMore) return;

    const root = scrollRootRef.current;
    const sentinel = sentinelRef.current;
    if (!root || !sentinel) return;

    const observer = new IntersectionObserver(
      (observerEntries) => {
        if (observerEntries.some((entry) => entry.isIntersecting)) {
          onLoadMore();
        }
      },
      { root, rootMargin: "120px", threshold: 0 },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, isLoading, isLoadingMore, onLoadMore]);

  if (isLoading) {
    return (
      <div className="h-full min-h-[min(78vh,44rem)] animate-pulse p-3">
        <div className="h-full rounded-lg bg-zinc-100 dark:bg-zinc-900" />
      </div>
    );
  }

  if (entries.length === 0) {
    return (
      <div className="min-h-[min(78vh,44rem)] p-4 text-sm text-zinc-500 dark:text-zinc-400">
        {emptyMessage}
      </div>
    );
  }

  return (
    <div
      ref={scrollRootRef}
      className="h-full max-h-[min(78vh,44rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600"
    >
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

      {loadMoreError ? (
        <div className="border-t border-zinc-100 px-3 py-3 dark:border-zinc-800">
          <p className="text-sm text-red-700 dark:text-red-300">{loadMoreError}</p>
          <button
            type="button"
            onClick={onRetryLoadMore}
            className="mt-2 text-sm font-medium text-zinc-700 underline-offset-2 hover:underline dark:text-zinc-300"
          >
            Retry
          </button>
        </div>
      ) : null}

      {isLoadingMore ? (
        <div className="px-3 py-3">
          <div className="h-10 animate-pulse rounded-md bg-zinc-100 dark:bg-zinc-900" />
        </div>
      ) : null}

      {hasMore ? <div ref={sentinelRef} className="h-1" aria-hidden /> : null}
    </div>
  );
}
