"use client";

import { useEffect, useMemo, useState } from "react";
import { BET_STATUS, type BetSlipListItem } from "@/features/bets/interfaces";
import { fetchBetSlips } from "@/features/bets/services/bets-api";
import { handleServiceError } from "@/lib/error-handler";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";

export function AgentPendingBetsDetailsPanel({
  selectedSeasonYears,
}: {
  selectedSeasonYears: string[];
}) {
  const [slips, setSlips] = useState<BetSlipListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const seasonYearsKey = selectedSeasonYears.join(",");

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    setError(null);

    fetchBetSlips(selectedSeasonYears)
      .then((data) => {
        if (!cancelled) setSlips(data);
      })
      .catch((caughtError) => {
        if (!cancelled) setError(handleServiceError(caughtError, "Failed to load pending bet slips."));
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [seasonYearsKey, selectedSeasonYears]);

  const pendingSlips = useMemo(
    () => slips.filter((slip) => slip.statusId === BET_STATUS.Pending),
    [slips]
  );

  if (isLoading) {
    return <div className="h-72 animate-pulse rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950" />;
  }

  if (error) {
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {error}
      </p>
    );
  }

  return (
    <section>
      <h4 className="mb-3 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        Pending Bet Slips
      </h4>
      <BetSlipList betSlips={pendingSlips} groupBySession={false} />
    </section>
  );
}
