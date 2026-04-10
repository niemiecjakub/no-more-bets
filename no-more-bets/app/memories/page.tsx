"use client";

import { useEffect, useState } from "react";
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
    <div className="animate-pulse space-y-4">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950"
        >
          <div className="border-b border-zinc-100 dark:border-zinc-800 px-4 py-3">
            <div className="h-5 w-48 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-64 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="space-y-2 px-4 py-3">
            <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function MemoriesPage() {
  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-4xl px-4 py-8 sm:px-6">
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
          <ul className="space-y-4">
            {memories.map((m) => (
              <li
                key={m.id}
                className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
              >
                <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
                  <h2 className="font-medium text-foreground">{m.name}</h2>
                  {m.description ? (
                    <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
                      {m.description}
                    </p>
                  ) : null}
                  <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-500">
                    Created {formatDate(m.createdAt)} · Updated{" "}
                    {formatDate(m.updatedAt)}
                  </p>
                </div>
                <div className="px-4 py-3">
                  <pre className="whitespace-pre-wrap break-words font-mono text-sm text-zinc-800 dark:text-zinc-200">
                    {m.content || "—"}
                  </pre>
                </div>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
