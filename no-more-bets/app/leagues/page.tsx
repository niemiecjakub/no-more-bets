"use client";

import { useEffect, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import { LeagueList } from "../../features/leagues/components/league-list";
import { LeagueTable } from "../../features/leagues/components/league-table";
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

function LeagueTableFallback() {
  return (
    <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {[1, 2, 3, 4, 5, 6].map((i) => (
        <div
          key={i}
          className="flex h-12 items-center gap-4 bg-white px-4 dark:bg-zinc-950"
        >
          <div className="h-4 w-8 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-12 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function LeaguesPage() {
  const [selectedLeagueId, setSelectedLeagueId] = useState<number | null>(null);
  const {
    leagues,
    leagueTableById,
    isLoading,
    error,
    isTableLoading,
    tableError,
    setLeagues,
    setLeagueTable,
  } = useLeagueStore();

  useEffect(() => {
    setLeagues();
  }, [setLeagues]);

  useEffect(() => {
    if (leagues.length === 0 || selectedLeagueId != null) return;
    setSelectedLeagueId(leagues[0].id);
  }, [leagues, selectedLeagueId]);

  useEffect(() => {
    if (selectedLeagueId == null) return;
    void setLeagueTable(selectedLeagueId);
  }, [selectedLeagueId, setLeagueTable]);

  const leagueTable =
    selectedLeagueId != null
      ? leagueTableById[selectedLeagueId]
      : undefined;

  const snapshotDate = leagueTable?.snapshotDate
    ? new Date(leagueTable.snapshotDate).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
      })
    : null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Leagues
        </h1>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(240px,320px)_1fr] lg:items-start">
          <div>
            {isLoading && leagues.length === 0 ? (
              <LeaguesFallback />
            ) : error ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {error}
              </p>
            ) : (
              <LeagueList
                leagues={leagues}
                selectedLeagueId={selectedLeagueId}
                onSelectLeague={setSelectedLeagueId}
              />
            )}
          </div>
          <div className="min-w-0">
            {selectedLeagueId == null ? (
              <p className="rounded-lg border border-dashed border-zinc-200 bg-white px-4 py-12 text-center text-zinc-500 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                Select a league to view the table.
              </p>
            ) : tableError ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {tableError}
              </p>
            ) : isTableLoading && !leagueTable ? (
              <>
                <div className="mb-1 h-8 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
                <LeagueTableFallback />
              </>
            ) : leagueTable ? (
              <>
                <h2 className="mb-1 flex items-center gap-3 text-2xl font-semibold tracking-tight text-foreground">
                  <SlugIcon
                    kind="league"
                    slug={leagueTable.leagueSlug}
                    alt={leagueTable.leagueName}
                    className="h-10 w-10"
                  />
                  {leagueTable.leagueName}
                </h2>
                {snapshotDate ? (
                  <p className="mb-6 text-sm text-zinc-500 dark:text-zinc-400">
                    Table as of {snapshotDate}
                  </p>
                ) : (
                  <div className="mb-6" />
                )}
                <LeagueTable data={leagueTable} />
              </>
            ) : null}
          </div>
        </div>
      </main>
    </div>
  );
}
