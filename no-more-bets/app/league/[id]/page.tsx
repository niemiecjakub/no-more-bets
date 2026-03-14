"use client";

import { notFound } from "next/navigation";
import { useParams } from "next/navigation";
import { useEffect } from "react";
import { LeagueTable } from "../../../features/leagues/components/league-table";
import { useLeagueStore } from "@/store/league-store";

function LeagueTableFallback() {
  return (
    <div className="animate-pulse space-y-3 rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden">
      {[1, 2, 3, 4, 5, 6].map((i) => (
        <div key={i} className="h-12 px-4 flex items-center gap-4 bg-white dark:bg-zinc-950">
          <div className="h-4 w-8 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 flex-1 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-12 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function LeaguePage() {
  const params = useParams();
  const id = params?.id as string | undefined;
  const leagueId = id != null && id !== "" ? Number(id) : NaN;
  const isValidId = !Number.isNaN(leagueId) && leagueId >= 1;

  const {
    leagueTableById,
    isLoading,
    error,
    setLeagueTable,
  } = useLeagueStore();

  useEffect(() => {
    if (!isValidId) return;
    setLeagueTable(leagueId);
  }, [leagueId, isValidId, setLeagueTable]);

  if (id != null && id !== "" && !isValidId) {
    notFound();
  }

  if (error?.includes("404")) {
    notFound();
  }

  const leagueTable = isValidId ? leagueTableById[leagueId] : undefined;

  if (isLoading && !leagueTable) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
        <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
          <div className="mb-1 h-8 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <LeagueTableFallback />
        </main>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
        <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        </main>
      </div>
    );
  }

  if (!leagueTable) {
    return null;
  }

  const snapshotDate = leagueTable.snapshotDate
    ? new Date(leagueTable.snapshotDate).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
      })
    : null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-1 text-2xl font-semibold tracking-tight text-foreground">
          {leagueTable.leagueName}
        </h1>
        {snapshotDate && (
          <p className="mb-6 text-sm text-zinc-500 dark:text-zinc-400">
            Table as of {snapshotDate}
          </p>
        )}
        {!snapshotDate && <div className="mb-6" />}
        <LeagueTable data={leagueTable} />
      </main>
    </div>
  );
}
