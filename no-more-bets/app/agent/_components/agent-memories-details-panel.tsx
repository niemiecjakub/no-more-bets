"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type { MemoryListItem } from "@/features/memories/interfaces";
import { fetchMemoriesPage } from "@/features/memories/services/memories-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentMemoriesList } from "./agent-memories-list";

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

function mergeMemories(existing: MemoryListItem[], incoming: MemoryListItem[]): MemoryListItem[] {
  const seen = new Set(existing.map((memory) => memory.id));
  const merged = [...existing];
  for (const memory of incoming) {
    if (seen.has(memory.id)) continue;
    seen.add(memory.id);
    merged.push(memory);
  }
  return merged;
}

function MemoriesFallback() {
  return (
    <div className="grid animate-pulse grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex flex-col gap-2 overflow-hidden rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="rounded-md border border-zinc-100 px-3 py-2.5 dark:border-zinc-800">
            <div className="h-4 w-3/4 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        ))}
      </div>
      <div className="min-h-[min(78vh,44rem)] overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="flex items-center justify-between gap-3 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
          <div className="h-6 w-2/5 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 w-36 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
        <div className="space-y-2 p-4">
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </div>
  );
}

export function AgentMemoriesDetailsPanel() {
  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [selectedMemoryId, setSelectedMemoryId] = useState<number | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<{ updatedAt: string; id: number } | null>(null);
  const [isLoadingMemories, setIsLoadingMemories] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const isLoadingMoreRef = useRef(false);

  const applyMemoriesPage = useCallback(
    (page: Awaited<ReturnType<typeof fetchMemoriesPage>>, append: boolean) => {
      setMemories((current) => (append ? mergeMemories(current, page.items) : page.items));
      setHasMore(page.hasMore);
      setNextCursor(
        page.hasMore && page.nextCursorUpdatedAt != null && page.nextCursorId != null
          ? { updatedAt: page.nextCursorUpdatedAt, id: page.nextCursorId }
          : null,
      );
    },
    [],
  );

  useEffect(() => {
    let cancelled = false;
    setIsLoadingMemories(true);
    setMemoriesError(null);
    setLoadMoreError(null);

    fetchMemoriesPage()
      .then((page) => {
        if (!cancelled) applyMemoriesPage(page, false);
      })
      .catch((error) => {
        if (!cancelled) {
          setMemoriesError(handleServiceError(error, "Failed to load memories."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoadingMemories(false);
      });

    return () => {
      cancelled = true;
    };
  }, [applyMemoriesPage]);

  const loadMore = useCallback(() => {
    if (!hasMore || !nextCursor || isLoadingMoreRef.current) return;

    isLoadingMoreRef.current = true;
    setIsLoadingMore(true);
    setLoadMoreError(null);

    fetchMemoriesPage({
      afterUpdatedAt: nextCursor.updatedAt,
      afterId: nextCursor.id,
    })
      .then((page) => {
        applyMemoriesPage(page, true);
      })
      .catch((error) => {
        setLoadMoreError(handleServiceError(error, "Failed to load more memories."));
      })
      .finally(() => {
        isLoadingMoreRef.current = false;
        setIsLoadingMore(false);
      });
  }, [applyMemoriesPage, hasMore, nextCursor]);

  useEffect(() => {
    setSelectedMemoryId((previous) => {
      if (memories.length === 0) return null;
      if (previous != null && memories.some((memory) => memory.id === previous)) return previous;
      return memories[0].id;
    });
  }, [memories]);

  const selectedMemory = selectedMemoryId != null ? memories.find((memory) => memory.id === selectedMemoryId) : undefined;

  if (isLoadingMemories && memories.length === 0) {
    return <MemoriesFallback />;
  }

  if (memoriesError) {
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {memoriesError}
      </p>
    );
  }

  if (memories.length === 0) {
    return (
      <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
        No memories saved yet.
      </p>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start lg:w-full">
        <AgentMemoriesList
          memories={memories}
          selectedMemoryId={selectedMemoryId}
          onSelectMemory={setSelectedMemoryId}
          isLoading={isLoadingMemories}
          hasMore={hasMore}
          isLoadingMore={isLoadingMore}
          onLoadMore={loadMore}
          loadMoreError={loadMoreError}
          onRetryLoadMore={loadMore}
        />
      </div>
      <div className="flex min-h-[min(78vh,44rem)] min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        {selectedMemory ? (
          <>
            <div className="flex min-w-0 shrink-0 items-center border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
              <div className="flex min-w-0 flex-1 items-baseline justify-between gap-3">
                <h2 className="min-w-0 flex-1 truncate text-lg font-semibold text-foreground">{selectedMemory.name}</h2>
                <span className="shrink-0 whitespace-nowrap text-right text-xs font-normal text-zinc-500 dark:text-zinc-500">
                  Updated {formatDate(selectedMemory.updatedAt)}
                </span>
              </div>
            </div>
            <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
              <pre className="wrap-break-word whitespace-pre-wrap font-mono text-sm text-zinc-800 dark:text-zinc-200">
                {selectedMemory.content || "—"}
              </pre>
            </div>
          </>
        ) : (
          <div className="flex flex-1 items-center justify-center p-6 text-sm text-zinc-500 dark:text-zinc-400">
            Select a memory to view its content.
          </div>
        )}
      </div>
    </div>
  );
}
