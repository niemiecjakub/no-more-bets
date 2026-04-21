"use client";

import { useEffect, useMemo, useState } from "react";
import { MatchList } from "../features/matches/components/match-list";
import { useMatchStore } from "@/store/match-store";
import { LeagueList } from "../features/leagues/components/league-list";
import { useLeagueStore } from "@/store/league-store";

const ALL_LEAGUES_ID = -1;

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

export default function Home() {
  const { matches, isLoading, error, setMatches } = useMatchStore();
  const [selectedLeagueId, setSelectedLeagueId] = useState<number>(ALL_LEAGUES_ID);
  const {
    leagues,
    isLoading: isLeaguesLoading,
    error: leaguesError,
    setLeagues,
  } = useLeagueStore();

  useEffect(() => {
    setMatches();
    setLeagues();
  }, [setLeagues, setMatches]);

  const leaguesWithAll = useMemo(
    () => [{ id: ALL_LEAGUES_ID, name: "All", slug: "all" }, ...leagues],
    [leagues]
  );

  const selectedLeague = useMemo(
    () =>
      selectedLeagueId === ALL_LEAGUES_ID
        ? null
        : leagues.find((league) => league.id === selectedLeagueId) ?? null,
    [leagues, selectedLeagueId]
  );

  const filteredMatches = useMemo(() => {
    if (!selectedLeague) return matches;

    return matches.filter((match) => {
      const sameSlug =
        match.leagueSlug &&
        selectedLeague.slug &&
        match.leagueSlug.toLowerCase() === selectedLeague.slug.toLowerCase();
      const sameName =
        match.leagueName &&
        selectedLeague.name &&
        match.leagueName.toLowerCase() === selectedLeague.name.toLowerCase();

      return sameSlug || sameName;
    });
  }, [matches, selectedLeague]);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Matches
        </h1>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(240px,320px)_1fr] lg:items-start">
          <div>
            {isLeaguesLoading && leagues.length === 0 ? (
              <LeaguesFallback />
            ) : leaguesError ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {leaguesError}
              </p>
            ) : (
              <LeagueList
                leagues={leaguesWithAll}
                selectedLeagueId={selectedLeagueId}
                onSelectLeague={setSelectedLeagueId}
              />
            )}
          </div>
          <div className="min-w-0">
            {isLoading && matches.length === 0 ? (
              <MatchesFallback />
            ) : error ? (
              <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">
                {error}
              </p>
            ) : (
              <MatchList matches={filteredMatches} />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
