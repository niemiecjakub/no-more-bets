"use client";

import { useEffect, useMemo, useState } from "react";
import { MatchList } from "../features/matches/components/match-list";
import { MATCH_STATUS } from "../features/matches/interfaces";
import { useMatchStore } from "@/store/match-store";
import { LeagueList } from "../features/leagues/components/league-list";
import { useLeagueStore } from "@/store/league-store";

const ALL_STATUSES_ID = -1;

const statusFilters = [
  { id: ALL_STATUSES_ID, label: "All" },
  { id: MATCH_STATUS.Upcoming, label: "Upcoming" },
  { id: MATCH_STATUS.Finished, label: "Finished" },
] as const;

function MatchesFallback() {
  return (
    <div className="animate-pulse space-y-5">
      {[1, 2].map((group) => (
        <section key={group} className="space-y-2">
          <div className="h-4 w-56 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
            {[1, 2, 3].map((row) => (
              <div
                key={`${group}-${row}`}
                className="space-y-2 border-b border-zinc-200 bg-white px-4 py-3 last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950"
              >
                <div className="mx-auto h-3 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-3">
                  <div className="ml-auto flex items-center gap-2">
                    <div className="h-6 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-7 w-7 rounded-full bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                  <div className="h-6 w-14 rounded bg-zinc-200 dark:bg-zinc-800" />
                  <div className="flex items-center gap-2">
                    <div className="h-7 w-7 rounded-full bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-6 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                </div>
                <div className="flex flex-wrap items-center justify-center gap-1.5 pt-0.5">
                  {[1, 2, 3, 4].map((chip) => (
                    <div
                      key={chip}
                      className="h-5 w-14 rounded-md bg-zinc-200 dark:bg-zinc-800"
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>
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
  const [selectedLeagueIds, setSelectedLeagueIds] = useState<number[]>([]);
  const [selectedStatusId, setSelectedStatusId] = useState<number>(MATCH_STATUS.Upcoming);
  const {
    leagues,
    isLoading: isLeaguesLoading,
    error: leaguesError,
    setLeagues,
  } = useLeagueStore();

  useEffect(() => {
    setLeagues();
  }, [setLeagues]);

  const matchFilters = useMemo(
    () => ({
      matchStatusId:
        selectedStatusId === ALL_STATUSES_ID ? undefined : selectedStatusId,
      leagueIds: selectedLeagueIds.length > 0 ? selectedLeagueIds : undefined,
    }),
    [selectedLeagueIds, selectedStatusId]
  );

  useEffect(() => {
    setMatches(matchFilters);
  }, [matchFilters, setMatches]);

  function handleToggleLeague(leagueId: number) {
    setSelectedLeagueIds((current) =>
      current.includes(leagueId)
        ? current.filter((id) => id !== leagueId)
        : [...current, leagueId]
    );
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleSelectStatus(statusId: number) {
    setSelectedStatusId(statusId);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Matches
        </h1>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(240px,320px)_1fr] lg:items-start">
          <div className="self-start lg:sticky lg:top-20">
            {isLeaguesLoading && leagues.length === 0 ? (
              <LeaguesFallback />
            ) : leaguesError ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {leaguesError}
              </p>
            ) : (
              <LeagueList
                leagues={leagues}
                selectedLeagueIds={selectedLeagueIds}
                onToggleLeague={handleToggleLeague}
              />
            )}
            <div className="mt-4 rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
                Match status
              </p>
              <div className="grid grid-cols-3 gap-2">
                {statusFilters.map((statusFilter) => {
                  const selected = statusFilter.id === selectedStatusId;
                  return (
                    <button
                      key={statusFilter.id}
                      type="button"
                      onClick={() => handleSelectStatus(statusFilter.id)}
                      aria-pressed={selected}
                      className={`rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                        selected
                          ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
                          : "bg-zinc-100 text-zinc-700 hover:bg-zinc-200 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
                      }`}
                    >
                      {statusFilter.label}
                    </button>
                  );
                })}
              </div>
            </div>
          </div>
          <div className="min-w-0">
            {isLoading ? (
              <MatchesFallback />
            ) : error ? (
              <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">
                {error}
              </p>
            ) : (
              <MatchList matches={matches} />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}
