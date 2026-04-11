"use client";

import { useEffect } from "react";
import { ClubList } from "../../features/clubs/components/club-list";
import { useClubStore } from "@/store/club-store";

function ClubsFallback() {
  return (
    <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {[1, 2, 3, 4, 5].map((i) => (
        <div
          key={i}
          className="flex h-14 items-center justify-between gap-2 bg-white px-4 dark:bg-zinc-950"
        >
          <div className="flex min-w-0 flex-1 items-center gap-2">
            <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function ClubsPage() {
  const { clubs, isLoading, error, setClubs } = useClubStore();

  useEffect(() => {
    setClubs();
  }, [setClubs]);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Clubs
        </h1>
        {isLoading && clubs.length === 0 ? (
          <ClubsFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        ) : (
          <ClubList clubs={clubs} />
        )}
      </main>
    </div>
  );
}
