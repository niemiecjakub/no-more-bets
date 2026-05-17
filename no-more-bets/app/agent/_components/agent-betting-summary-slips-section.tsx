"use client";

import { useEffect, useRef } from "react";
import type { BetSlipListItem } from "@/features/bets/interfaces";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";

interface AgentBettingSummarySlipsSectionProps {
  slips: BetSlipListItem[];
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
  loadMoreError: string | null;
  onRetryLoadMore: () => void;
}

export function AgentBettingSummarySlipsSection({
  slips,
  isLoading,
  hasMore,
  isLoadingMore,
  onLoadMore,
  loadMoreError,
  onRetryLoadMore,
}: AgentBettingSummarySlipsSectionProps) {
  const sentinelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isLoading || isLoadingMore || !hasMore) return;

    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          onLoadMore();
        }
      },
      { root: null, rootMargin: "200px", threshold: 0 },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, isLoading, isLoadingMore, onLoadMore]);

  if (isLoading) {
    return (
      <div className="h-48 animate-pulse rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950" />
    );
  }

  if (slips.length === 0) {
    return (
      <p className="py-12 text-center text-sm text-zinc-500 dark:text-zinc-400">
        No settled betting slips yet.
      </p>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <BetSlipList betSlips={slips} groupBySession={false} />

      {loadMoreError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 dark:border-red-900 dark:bg-red-950/30">
          <p className="text-sm text-red-800 dark:text-red-200">{loadMoreError}</p>
          <button
            type="button"
            onClick={onRetryLoadMore}
            className="mt-2 text-sm font-medium text-red-900 underline-offset-2 hover:underline dark:text-red-100"
          >
            Retry
          </button>
        </div>
      ) : null}

      {isLoadingMore ? (
        <div className="h-12 animate-pulse rounded-lg bg-zinc-100 dark:bg-zinc-900" />
      ) : null}

      {hasMore ? <div ref={sentinelRef} className="h-1" aria-hidden /> : null}
    </div>
  );
}
