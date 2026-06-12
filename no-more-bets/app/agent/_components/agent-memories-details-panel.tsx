"use client";

import { useCallback, useEffect, useId, useLayoutEffect, useRef, useState } from "react";
import type { MemoryListItem } from "@/features/memories/interfaces";
import { fetchMemoriesPage } from "@/features/memories/services/memories-api";
import { handleServiceError } from "@/lib/error-handler";
import { cn } from "@/lib/utils";
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

function isMemoryDeleted(memory: MemoryListItem): boolean {
  return memory.deletedAt != null;
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
  const showDeletedCheckboxId = useId();
  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [selectedMemoryId, setSelectedMemoryId] = useState<number | null>(null);
  const [showDeleted, setShowDeleted] = useState(false);
  const [hasMore, setHasMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<{ at: string; id: number } | null>(null);
  const [isLoadingMemories, setIsLoadingMemories] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
  const isLoadingMoreRef = useRef(false);
  const detailPanelRef = useRef<HTMLDivElement>(null);
  const shouldScrollToDetailRef = useRef(false);

  const applyMemoriesPage = useCallback(
    (page: Awaited<ReturnType<typeof fetchMemoriesPage>>, append: boolean) => {
      setMemories((current) => (append ? mergeMemories(current, page.items) : page.items));
      setHasMore(page.hasMore);
      setNextCursor(
        page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
          ? { at: page.nextCursorAt, id: page.nextCursorId }
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
    setMemories([]);
    setHasMore(false);
    setNextCursor(null);

    fetchMemoriesPage({ includeDeleted: showDeleted })
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
  }, [applyMemoriesPage, showDeleted]);

  const loadMore = useCallback(() => {
    if (!hasMore || !nextCursor || isLoadingMoreRef.current) return;

    isLoadingMoreRef.current = true;
    setIsLoadingMore(true);
    setLoadMoreError(null);

    fetchMemoriesPage({
      afterUpdatedAt: nextCursor.at,
      afterId: nextCursor.id,
      includeDeleted: showDeleted,
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
  }, [applyMemoriesPage, hasMore, nextCursor, showDeleted]);

  useEffect(() => {
    setSelectedMemoryId((previous) => {
      if (memories.length === 0) return null;
      if (previous != null && memories.some((memory) => memory.id === previous)) return previous;
      return memories[0].id;
    });
  }, [memories]);

  useLayoutEffect(() => {
    if (!shouldScrollToDetailRef.current) return;
    shouldScrollToDetailRef.current = false;
    if (window.matchMedia("(min-width: 1024px)").matches) return;
    detailPanelRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [selectedMemoryId]);

  const selectedMemory = selectedMemoryId != null ? memories.find((memory) => memory.id === selectedMemoryId) : undefined;
  const selectedMemoryIsDeleted = selectedMemory != null && isMemoryDeleted(selectedMemory);

  function selectMemory(memoryId: number) {
    shouldScrollToDetailRef.current = true;
    setSelectedMemoryId(memoryId);
  }

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

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start lg:w-full">
        <div className="border-b border-zinc-100 px-3 py-3 dark:border-zinc-800">
          <label
            htmlFor={showDeletedCheckboxId}
            className="flex cursor-pointer items-center gap-2 text-sm text-zinc-700 dark:text-zinc-300"
          >
            <input
              id={showDeletedCheckboxId}
              type="checkbox"
              checked={showDeleted}
              onChange={(event) => setShowDeleted(event.target.checked)}
              className="size-4 rounded border-zinc-300 text-zinc-900 focus:ring-zinc-400 dark:border-zinc-600 dark:bg-zinc-900 dark:focus:ring-zinc-500"
            />
            Show deleted memories
          </label>
        </div>
        <AgentMemoriesList
          memories={memories}
          selectedMemoryId={selectedMemoryId}
          onSelectMemory={selectMemory}
          isLoading={isLoadingMemories}
          hasMore={hasMore}
          isLoadingMore={isLoadingMore}
          onLoadMore={loadMore}
          loadMoreError={loadMoreError}
          onRetryLoadMore={loadMore}
          emptyMessage={showDeleted ? "No memories found." : "No memories saved yet."}
        />
      </div>
      <div
        ref={detailPanelRef}
        className="flex min-h-[min(78vh,44rem)] min-w-0 scroll-mt-20 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
      >
        {selectedMemory ? (
          <>
            <div className="flex min-w-0 shrink-0 items-center border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
              <div className="flex min-w-0 flex-1 items-baseline justify-between gap-3">
                <h2
                  className={cn(
                    "min-w-0 flex-1 truncate text-lg font-semibold text-foreground",
                    selectedMemoryIsDeleted && "text-zinc-500 line-through dark:text-zinc-400",
                  )}
                >
                  {selectedMemory.name}
                </h2>
                <span className="shrink-0 whitespace-nowrap text-right text-xs font-normal text-zinc-500 dark:text-zinc-500">
                  {selectedMemoryIsDeleted && selectedMemory.deletedAt
                    ? `Deleted ${formatDate(selectedMemory.deletedAt)}`
                    : `Updated ${formatDate(selectedMemory.updatedAt)}`}
                </span>
              </div>
            </div>
            {selectedMemoryIsDeleted ? (
              <div className="border-b border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-200">
                This memory was deleted and is no longer used by the agent.
              </div>
            ) : null}
            <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
              <pre
                className={cn(
                  "wrap-break-word whitespace-pre-wrap font-mono text-sm text-zinc-800 dark:text-zinc-200",
                  selectedMemoryIsDeleted && "opacity-70 text-zinc-500 dark:text-zinc-400",
                )}
              >
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
