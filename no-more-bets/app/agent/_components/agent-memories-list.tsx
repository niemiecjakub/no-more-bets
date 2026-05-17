"use client";

import { useEffect, useRef } from "react";
import type { MemoryListItem } from "@/features/memories/interfaces";

interface AgentMemoriesListProps {
  memories: MemoryListItem[];
  selectedMemoryId: number | null;
  onSelectMemory: (memoryId: number) => void;
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
  loadMoreError: string | null;
  onRetryLoadMore: () => void;
}

function formatDate(iso: string) {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: "medium",
      timeStyle: "short",
      hour12: false,
    });
  } catch {
    return iso;
  }
}

export function AgentMemoriesList({
  memories,
  selectedMemoryId,
  onSelectMemory,
  isLoading,
  hasMore,
  isLoadingMore,
  onLoadMore,
  loadMoreError,
  onRetryLoadMore,
}: AgentMemoriesListProps) {
  const scrollRootRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isLoading || isLoadingMore || !hasMore) return;

    const root = scrollRootRef.current;
    const sentinel = sentinelRef.current;
    if (!root || !sentinel) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
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
        <div className="space-y-2">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-16 rounded-md bg-zinc-100 dark:bg-zinc-900" />
          ))}
        </div>
      </div>
    );
  }

  if (memories.length === 0) {
    return (
      <p className="p-4 text-sm text-zinc-500 dark:text-zinc-400">No memories saved yet.</p>
    );
  }

  return (
    <div
      ref={scrollRootRef}
      className="h-full max-h-[min(78vh,44rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600"
    >
      <ul className="min-w-0 space-y-1 p-2">
        {memories.map((memory) => {
          const isSelected = memory.id === selectedMemoryId;
          return (
            <li key={memory.id} className="min-w-0">
              <button
                type="button"
                onClick={() => onSelectMemory(memory.id)}
                className={
                  "min-w-0 max-w-full rounded-md border px-3 py-2.5 text-left transition-colors " +
                  (isSelected
                    ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                    : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                }
              >
                <span className="line-clamp-2 min-w-0 max-w-full break-all wrap-break-word font-medium text-foreground">
                  {memory.name}
                </span>
                {memory.description ? (
                  <span className="mt-1 block min-w-0 max-w-full line-clamp-2 wrap-break-word text-sm text-zinc-600 dark:text-zinc-400">
                    {memory.description}
                  </span>
                ) : null}
                <span className="mt-2 block min-w-0 max-w-full truncate text-xs text-zinc-500 dark:text-zinc-500">
                  Updated {formatDate(memory.updatedAt)}
                </span>
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
