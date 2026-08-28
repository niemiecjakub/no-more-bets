"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { BetSlipListItem } from "../interfaces";
import { fetchDailyPicksPage } from "../services/bets-api";
import type { PagedResponse } from "@/lib/paged-response";
import { DailyPicksGrid } from "./daily-picks-row";
import { handleServiceError } from "@/lib/error-handler";
import { parseApiDate } from "@/utils/format-date";

const PAGE_SIZE = 7;

interface DailyPicksListProps {
  initialPage: PagedResponse<BetSlipListItem>;
}

interface DateGroup {
  key: string;
  slips: BetSlipListItem[];
}

function slipDateKey(slip: BetSlipListItem): string | null {
  if (!slip.slipDate) return null;
  return String(slip.slipDate).slice(0, 10);
}

function groupSlipsByDate(slips: BetSlipListItem[]): DateGroup[] {
  const groups: DateGroup[] = [];
  for (const slip of slips) {
    const key = slipDateKey(slip);
    if (!key) continue;
    const existing = groups.find((group) => group.key === key);
    if (existing) {
      existing.slips.push(slip);
    } else {
      groups.push({ key, slips: [slip] });
    }
  }
  return groups;
}

function nextAfterDate(page: PagedResponse<BetSlipListItem>): string | null {
  if (!page.hasMore) return null;
  for (let i = page.items.length - 1; i >= 0; i--) {
    const key = slipDateKey(page.items[i]);
    if (key) return key;
  }
  return page.nextCursorAt?.slice(0, 10) ?? null;
}

function formatDateHeading(dateKey: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "long",
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(parseApiDate(dateKey));
}

function PicksFallback() {
  return (
    <div className="flex flex-col gap-8">
      {[1, 2].map((group) => (
        <section key={group} className="flex flex-col gap-3">
          <div className="h-4 w-48 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            {[1, 2, 3].map((col) => (
              <div
                key={col}
                className="h-48 rounded-lg border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900"
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

export function DailyPicksList({ initialPage }: DailyPicksListProps) {
  const [slips, setSlips] = useState(initialPage.items);
  const [hasMore, setHasMore] = useState(initialPage.hasMore);
  const [afterDate, setAfterDate] = useState(nextAfterDate(initialPage));
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const isLoadingMoreRef = useRef(false);
  const sentinelRef = useRef<HTMLDivElement>(null);

  const groups = useMemo(() => groupSlipsByDate(slips), [slips]);

  const loadMore = useCallback(async () => {
    if (!hasMore || !afterDate || isLoadingMoreRef.current) return;
    isLoadingMoreRef.current = true;
    setIsLoadingMore(true);
    setLoadMoreError(null);
    try {
      const page = await fetchDailyPicksPage({ limit: PAGE_SIZE, afterDate });
      setSlips((current) => [...current, ...page.items]);
      setHasMore(page.hasMore);
      setAfterDate(nextAfterDate(page));
    } catch (err) {
      setLoadMoreError(handleServiceError(err, "Failed to load more picks."));
    } finally {
      isLoadingMoreRef.current = false;
      setIsLoadingMore(false);
    }
  }, [afterDate, hasMore]);

  useEffect(() => {
    if (!hasMore || isLoadingMore || !afterDate) return;
    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          void loadMore();
        }
      },
      { root: null, rootMargin: "200px", threshold: 0 },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [afterDate, hasMore, isLoadingMore, loadMore]);

  if (slips.length === 0) {
    return <p className="py-12 text-center text-zinc-500 dark:text-zinc-400">No daily picks yet.</p>;
  }

  return (
    <div className="flex flex-col gap-8">
      {groups.map((group) => (
        <section key={group.key} className="flex flex-col gap-3">
          <h2 className="px-1 text-sm font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            {formatDateHeading(group.key)}
          </h2>
          <DailyPicksGrid slips={group.slips} />
        </section>
      ))}
      {loadMoreError ? (
        <div className="flex flex-col items-center gap-2">
          <p className="text-sm text-red-700 dark:text-red-300">{loadMoreError}</p>
          <button
            type="button"
            onClick={() => void loadMore()}
            className="rounded-md border border-zinc-300 bg-white px-3 py-1.5 text-sm font-medium text-foreground hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
          >
            Retry
          </button>
        </div>
      ) : null}
      {isLoadingMore ? (
        <div className="animate-pulse">
          <PicksFallback />
        </div>
      ) : null}
      {hasMore ? <div ref={sentinelRef} className="h-1" aria-hidden /> : null}
    </div>
  );
}

