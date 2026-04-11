"use client";

import { useEffect, useMemo, useState } from "react";
import { fetchMemories } from "../../features/memories/services/memories-api";
import type { MemoryListItem } from "../../features/memories/interfaces";
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
    <div className="grid min-h-[min(70vh,36rem)] animate-pulse grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)]">
      <div className="flex flex-col gap-2 overflow-hidden rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
        {[1, 2, 3, 4].map((i) => (
          <div
            key={i}
            className="rounded-md border border-zinc-100 px-3 py-2.5 dark:border-zinc-800"
          >
            <div className="h-4 w-3/4 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        ))}
      </div>
      <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
          <div className="h-6 w-2/3 rounded bg-zinc-200 dark:bg-zinc-800" />
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

export default function MemoriesPage() {
  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const sortedMemories = useMemo(
    () =>
      [...memories].sort(
        (a, b) =>
          new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
      ),
    [memories],
  );

  useEffect(() => {
    setSelectedId((prev) => {
      if (sortedMemories.length === 0) return null;
      if (prev != null && sortedMemories.some((m) => m.id === prev)) {
        return prev;
      }
      return sortedMemories[0].id;
    });
  }, [sortedMemories]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await fetchMemories();
        if (!cancelled) {
          setMemories(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(handleServiceError(err, "Failed to load memories."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const selectedMemory =
    selectedId != null
      ? sortedMemories.find((m) => m.id === selectedId)
      : undefined;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Memories
        </h1>
        {isLoading && memories.length === 0 ? (
          <MemoriesFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        ) : memories.length === 0 ? (
          <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
            No memories saved yet.
          </p>
        ) : (
          <div className="grid min-h-[min(70vh,36rem)] grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)]">
            <div className="flex min-h-0 min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
              <ul className="min-h-0 min-w-0 flex-1 space-y-1 overflow-y-auto p-2">
                {sortedMemories.map((m) => {
                  const isSelected = m.id === selectedId;
                  return (
                    <li key={m.id} className="min-w-0">
                      <button
                        type="button"
                        onClick={() => setSelectedId(m.id)}
                        className={
                          "min-w-0 max-w-full rounded-md border px-3 py-2.5 text-left transition-colors " +
                          (isSelected
                            ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                            : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                        }
                      >
                        <span className="line-clamp-2 min-w-0 max-w-full wrap-break-word break-all font-medium text-foreground">
                          {m.name}
                        </span>
                        {m.description ? (
                          <span className="mt-1 line-clamp-2 block min-w-0 max-w-full wrap-break-word text-sm text-zinc-600 dark:text-zinc-400">
                            {m.description}
                          </span>
                        ) : null}
                        <span className="mt-2 block min-w-0 max-w-full truncate text-xs text-zinc-500 dark:text-zinc-500">
                          Updated {formatDate(m.updatedAt)}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>
            <div className="flex min-h-0 min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
              {selectedMemory ? (
                <>
                  <div className="min-w-0 shrink-0 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
                    <h2 className="min-w-0 wrap-break-word break-all text-lg font-semibold text-foreground">
                      {selectedMemory.name}
                    </h2>
                    <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-500">
                      Updated {formatDate(selectedMemory.updatedAt)}
                    </p>
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
        )}
      </main>
    </div>
  );
}
