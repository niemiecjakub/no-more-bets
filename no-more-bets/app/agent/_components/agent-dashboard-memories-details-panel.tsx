"use client";

import { useEffect, useMemo, useState } from "react";
import type { MemoryListItem } from "@/features/memories/interfaces";
import { fetchMemories } from "@/features/memories/services/memories-api";
import { handleServiceError } from "@/lib/error-handler";

function formatDate(iso: string) {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: "medium",
      timeStyle: "short",
    });
  } catch {
    return iso;
  }
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
      <div className="min-h-[min(70vh,36rem)] overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
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

export function AgentDashboardMemoriesDetailsPanel() {
  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [selectedMemoryId, setSelectedMemoryId] = useState<number | null>(null);
  const [isLoadingMemories, setIsLoadingMemories] = useState(true);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);

  const sortedMemories = useMemo(
    () => [...memories].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()),
    [memories]
  );

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoadingMemories(true);
      setMemoriesError(null);
      try {
        const data = await fetchMemories();
        if (!cancelled) {
          setMemories(data);
        }
      } catch (error) {
        if (!cancelled) {
          setMemoriesError(handleServiceError(error, "Failed to load memories."));
        }
      } finally {
        if (!cancelled) {
          setIsLoadingMemories(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setSelectedMemoryId((previous) => {
      if (sortedMemories.length === 0) return null;
      if (previous != null && sortedMemories.some((memory) => memory.id === previous)) return previous;
      return sortedMemories[0].id;
    });
  }, [sortedMemories]);

  const selectedMemory = selectedMemoryId != null ? sortedMemories.find((memory) => memory.id === selectedMemoryId) : undefined;

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
        <div className="h-full max-h-[min(70vh,36rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600">
          <ul className="min-w-0 space-y-1 p-2">
            {sortedMemories.map((memory) => {
              const isSelected = memory.id === selectedMemoryId;
              return (
                <li key={memory.id} className="min-w-0">
                  <button
                    type="button"
                    onClick={() => setSelectedMemoryId(memory.id)}
                    className={
                      "min-w-0 max-w-full rounded-md border px-3 py-2.5 text-left transition-colors " +
                      (isSelected
                        ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                        : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                    }
                  >
                    <span className="line-clamp-2 min-w-0 max-w-full break-all wrap-break-word font-medium text-foreground">{memory.name}</span>
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
        </div>
      </div>
      <div className="flex min-h-[min(70vh,36rem)] min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
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
