"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type {
  BankrollEntryBetDetailsDto,
  BankrollEntryListItemDto,
} from "@/features/bets/interfaces";
import {
  fetchBankrollEntriesPage,
  fetchBankrollEntryBetDetails,
} from "@/features/bets/services/bankroll-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentBankrollEntriesList } from "./agent-bankroll-entries-list";
import { AgentBankrollRelatedBet } from "./agent-bankroll-related-bet";

function mergeEntries(
  existing: BankrollEntryListItemDto[],
  incoming: BankrollEntryListItemDto[],
): BankrollEntryListItemDto[] {
  const seen = new Set(existing.map((entry) => entry.id));
  const merged = [...existing];
  for (const entry of incoming) {
    if (seen.has(entry.id)) continue;
    seen.add(entry.id);
    merged.push(entry);
  }
  return merged;
}

export function AgentBankrollDetailsPanel() {
  const [entries, setEntries] = useState<BankrollEntryListItemDto[]>([]);
  const [selectedEntry, setSelectedEntry] = useState<BankrollEntryListItemDto | null>(null);
  const [selectedBetDetails, setSelectedBetDetails] = useState<BankrollEntryBetDetailsDto | null>(null);

  const [hasMore, setHasMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<{ at: string; id: number } | null>(null);

  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [isBetDetailsLoading, setIsBetDetailsLoading] = useState(false);

  const [entriesError, setEntriesError] = useState<string | null>(null);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const [betDetailsError, setBetDetailsError] = useState<string | null>(null);

  const isLoadingMoreRef = useRef(false);

  const loadBetDetailsForEntry = useCallback((entry: BankrollEntryListItemDto | null) => {
    setSelectedEntry(entry);
    setSelectedBetDetails(null);
    setBetDetailsError(null);

    if (!entry || !entry.betId) {
      setIsBetDetailsLoading(false);
      return;
    }

    setIsBetDetailsLoading(true);
    fetchBankrollEntryBetDetails(entry.id)
      .then((data) => {
        setSelectedBetDetails(data);
      })
      .catch((caughtError) => {
        setSelectedBetDetails(null);
        setBetDetailsError(handleServiceError(caughtError, "Failed to load related bet."));
      })
      .finally(() => {
        setIsBetDetailsLoading(false);
      });
  }, []);

  const applyPage = useCallback((page: Awaited<ReturnType<typeof fetchBankrollEntriesPage>>, append: boolean) => {
    setEntries((current) => (append ? mergeEntries(current, page.items) : page.items));
    setHasMore(page.hasMore);
    setNextCursor(
      page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
        ? { at: page.nextCursorAt, id: page.nextCursorId }
        : null,
    );
  }, []);

  useEffect(() => {
    let cancelled = false;

    setIsInitialLoading(true);
    setEntriesError(null);
    setLoadMoreError(null);

    fetchBankrollEntriesPage()
      .then((page) => {
        if (cancelled) return;
        applyPage(page, false);
        const firstWithBet = page.items.find((entry) => entry.betId !== null) ?? page.items[0] ?? null;
        loadBetDetailsForEntry(firstWithBet);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setEntriesError(handleServiceError(caughtError, "Failed to load bankroll entries."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsInitialLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [applyPage, loadBetDetailsForEntry]);

  const loadMore = useCallback(() => {
    if (!hasMore || !nextCursor || isLoadingMoreRef.current) return;

    isLoadingMoreRef.current = true;
    setIsLoadingMore(true);
    setLoadMoreError(null);

    fetchBankrollEntriesPage({
      afterCreatedAt: nextCursor.at,
      afterId: nextCursor.id,
    })
      .then((page) => {
        applyPage(page, true);
      })
      .catch((caughtError) => {
        setLoadMoreError(handleServiceError(caughtError, "Failed to load more entries."));
      })
      .finally(() => {
        isLoadingMoreRef.current = false;
        setIsLoadingMore(false);
      });
  }, [applyPage, hasMore, nextCursor]);

  return (
    <section className="flex flex-col gap-4">
      <div className="grid min-h-[min(78vh,44rem)] grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
        <section className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
          {entriesError ? (
            <p className="m-4 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
              {entriesError}
            </p>
          ) : (
            <AgentBankrollEntriesList
              entries={entries}
              selectedEntryId={selectedEntry?.id ?? null}
              onSelectEntry={loadBetDetailsForEntry}
              isLoading={isInitialLoading}
              hasMore={hasMore}
              isLoadingMore={isLoadingMore}
              onLoadMore={loadMore}
              loadMoreError={loadMoreError}
              onRetryLoadMore={loadMore}
            />
          )}
        </section>

        <section className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
          <AgentBankrollRelatedBet
            selectedEntry={selectedEntry}
            details={selectedBetDetails}
            isLoading={isBetDetailsLoading}
            error={betDetailsError}
          />
        </section>
      </div>
    </section>
  );
}
