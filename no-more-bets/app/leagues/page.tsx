"use client";

import { useEffect } from "react";
import { LeagueList } from "../../features/leagues/components/league-list";
import { useLeagueStore } from "@/store/league-store";

function LeaguesFallback() {
  return (
    <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {[1, 2, 3, 4, 5].map((i) => (
        <div
          key={i}
          className="flex h-12 items-center gap-2 bg-white px-4 dark:bg-zinc-950"
        >
          <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function LeaguesPage() {
  const { leagues, isLoading, error, setLeagues } = useLeagueStore();

  useEffect(() => {
    setLeagues();
  }, [setLeagues]);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Leagues
        </h1>
        {isLoading && leagues.length === 0 ? (
          <LeaguesFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        ) : (
          <LeagueList leagues={leagues} />
        )}
      </main>
    </div>
  );
}
