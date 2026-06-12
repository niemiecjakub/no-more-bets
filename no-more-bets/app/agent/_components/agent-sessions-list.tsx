"use client";

import { useEffect, useLayoutEffect, useRef } from "react";
import type { AgentSessionListItem } from "@/features/sessions/interfaces";
import { sessionPhaseIcon } from "@/features/sessions/agent-session-phases";

interface AgentSessionsListProps {
  sessions: AgentSessionListItem[];
  selectedSessionId: number | null;
  onSelectSession: (sessionId: number) => void;
  isLoading: boolean;
  hasMore: boolean;
  isLoadingMore: boolean;
  onLoadMore: () => void;
  loadMoreError: string | null;
  onRetryLoadMore: () => void;
  emptyMessage?: string;
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

export function AgentSessionsList({
  sessions,
  selectedSessionId,
  onSelectSession,
  isLoading,
  hasMore,
  isLoadingMore,
  onLoadMore,
  loadMoreError,
  onRetryLoadMore,
  emptyMessage = "No agent sessions recorded yet.",
}: AgentSessionsListProps) {
  const scrollRootRef = useRef<HTMLDivElement>(null);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const scrollTopRef = useRef(0);

  useEffect(() => {
    const root = scrollRootRef.current;
    if (!root) return;

    const handleScroll = () => {
      scrollTopRef.current = root.scrollTop;
    };

    handleScroll();
    root.addEventListener("scroll", handleScroll, { passive: true });
    return () => root.removeEventListener("scroll", handleScroll);
  }, [isLoading, sessions.length]);

  useLayoutEffect(() => {
    if (isLoading) {
      scrollTopRef.current = 0;
      return;
    }

    const root = scrollRootRef.current;
    if (!root) return;
    root.scrollTop = scrollTopRef.current;
  }, [isLoading, isLoadingMore, selectedSessionId, sessions.length]);

  function handleSelectSession(sessionId: number) {
    if (scrollRootRef.current) {
      scrollTopRef.current = scrollRootRef.current.scrollTop;
    }
    onSelectSession(sessionId);
  }

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

  if (sessions.length === 0) {
    return (
      <div className="p-4 text-sm text-zinc-500 dark:text-zinc-400">
        {emptyMessage}
      </div>
    );
  }

  return (
    <div
      ref={scrollRootRef}
      className="h-full max-h-[min(78vh,44rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600"
    >
      <ul className="w-full min-w-0 space-y-1 p-2">
        {sessions.map((session) => {
          const isSelected = session.id === selectedSessionId;
          const PhaseIcon = sessionPhaseIcon(session.phaseId);
          return (
            <li key={session.id} className="w-full min-w-0">
              <button
                type="button"
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => handleSelectSession(session.id)}
                className={
                  "flex w-full min-w-0 max-w-full gap-2.5 rounded-md border px-3 py-2.5 text-left transition-colors " +
                  (isSelected
                    ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                    : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                }
              >
                <PhaseIcon
                  className={
                    "mt-0.5 h-4 w-4 shrink-0 " +
                    (isSelected ? "text-zinc-700 dark:text-zinc-300" : "text-zinc-400 dark:text-zinc-500")
                  }
                  aria-hidden
                />
                <div className="min-w-0 flex-1">
                  <span className="block min-w-0 truncate font-medium text-foreground">{session.phaseName}</span>
                  <div className="mt-1 flex min-w-0 items-center gap-2">
                    <span className="min-w-0 flex-1 truncate text-left text-xs text-zinc-500 dark:text-zinc-500">
                      {formatDate(session.startedAt)}
                    </span>
                    <span className="shrink-0 text-right text-xs font-normal tabular-nums text-zinc-500 dark:text-zinc-500">
                      Session #{session.id}
                    </span>
                  </div>
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
