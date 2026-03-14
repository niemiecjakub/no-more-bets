"use client";

import { useEffect } from "react";
import { BetSlipList } from "../../features/bets/components/bet-slip-list";
import { useBetSlipStore } from "@/store/bet-slip-store";

function BetsFallback() {
  return (
    <div className="animate-pulse space-y-4">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950"
        >
          <div className="flex gap-2 px-4 py-3 border-b border-zinc-100 dark:border-zinc-800">
            <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-5 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="grid grid-cols-3 gap-3 px-4 py-3 border-b border-zinc-100 dark:border-zinc-800">
            <div className="h-8 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-8 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-8 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="space-y-2 px-4 py-3">
            <div className="h-4 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function BetsPage() {
  const { betSlips, isLoading, error, setBetSlips } = useBetSlipStore();

  useEffect(() => {
    setBetSlips();
  }, [setBetSlips]);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Bets
        </h1>
        {isLoading && betSlips.length === 0 ? (
          <BetsFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        ) : (
          <BetSlipList betSlips={betSlips} />
        )}
      </main>
    </div>
  );
}
