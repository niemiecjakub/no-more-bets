"use client";

import { useEffect } from "react";
import { MatchList } from "../features/matches/components/match-list";
import { useMatchStore } from "@/store/match-store";

function MatchesFallback() {
  return (
    <div className="animate-pulse space-y-3 rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="h-14 px-4 flex items-center gap-4 bg-white dark:bg-zinc-950">
          <div className="h-4 flex-1 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function Home() {
  const { matches, isLoading, error, setMatches } = useMatchStore();

  useEffect(() => {
    setMatches();
  }, [setMatches]);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Matches
        </h1>
        {isLoading && matches.length === 0 ? (
          <MatchesFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">
            {error}
          </p>
        ) : (
          <MatchList matches={matches} />
        )}
      </main>
    </div>
  );
}
