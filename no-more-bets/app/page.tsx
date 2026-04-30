"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { MatchList } from "../features/matches/components/match-list";
import { MATCH_STATUS } from "../features/matches/interfaces";
import { useMatchStore } from "@/store/match-store";
import { LeagueList } from "../features/leagues/components/league-list";
import { useLeagueStore } from "@/store/league-store";
import {
  fetchAgentDashboardBankrollWidget,
  fetchAgentDashboardBettingSummaryWidget,
  fetchAgentDashboardPendingBetsWidget,
} from "@/features/bets/services/agent-dashboard-api";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardPendingBetsWidget,
} from "@/features/bets/interfaces";
import { handleServiceError } from "@/lib/error-handler";
import { formatCurrency } from "@/utils/format-currency";

const ALL_STATUSES_ID = -1;

const statusFilters = [
  { id: ALL_STATUSES_ID, label: "All" },
  { id: MATCH_STATUS.Upcoming, label: "Upcoming" },
  { id: MATCH_STATUS.Finished, label: "Finished" },
] as const;

function StatCard({
  label,
  value,
  helper,
}: {
  label: string;
  value: string;
  helper?: string;
}) {
  return (
    <article className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
      <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        {label}
      </p>
      <p className="mt-1 text-xl font-semibold tabular-nums tracking-tight text-foreground">
        {value}
      </p>
      {helper ? (
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">{helper}</p>
      ) : null}
    </article>
  );
}

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
    <div className="grid animate-pulse grid-cols-1 gap-3">
      {[1, 2, 3, 4, 5].map((i) => (
        <div
          key={i}
          className="relative overflow-hidden rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950"
        >
          <div className="absolute left-0 top-0 h-1 w-full bg-zinc-200 dark:bg-zinc-800" />
          <div className="mt-1 flex items-center gap-3">
            <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function Home() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { matches, isLoading, error, setMatches } = useMatchStore();
  const {
    leagues,
    isLoading: isLeaguesLoading,
    error: leaguesError,
    setLeagues,
  } = useLeagueStore();
  const [bankrollWidget, setBankrollWidget] =
    useState<AgentDashboardBankrollWidget | null>(null);
  const [summaryWidget, setSummaryWidget] =
    useState<AgentDashboardBettingSummaryWidget | null>(null);
  const [pendingWidget, setPendingWidget] =
    useState<AgentDashboardPendingBetsWidget | null>(null);
  const [isStatsLoading, setIsStatsLoading] = useState(false);
  const [statsError, setStatsError] = useState<string | null>(null);

  const { selectedLeagueIds, selectedStatusId, matchFilters } = useMemo(() => {
    const statusParam = Number(searchParams.get("status"));
    const matchedStatus = statusFilters.find(
      (statusFilter) => statusFilter.id === statusParam
    );
    const parsedStatusId = matchedStatus?.id ?? MATCH_STATUS.Upcoming;

    const leaguesParam = searchParams.get("leagues");
    const parsedLeagueIds = leaguesParam
      ? leaguesParam
          .split(",")
          .map((item) => Number(item.trim()))
          .filter((id) => Number.isInteger(id) && id > 0)
      : [];

    return {
      selectedLeagueIds: parsedLeagueIds,
      selectedStatusId: parsedStatusId,
      matchFilters: {
        matchStatusId:
          parsedStatusId === ALL_STATUSES_ID ? undefined : parsedStatusId,
        leagueIds: parsedLeagueIds.length > 0 ? parsedLeagueIds : undefined,
      },
    };
  }, [searchParams]);

  useEffect(() => {
    setLeagues();
  }, [setLeagues]);

  useEffect(() => {
    setMatches(matchFilters);
  }, [matchFilters, setMatches]);

  useEffect(() => {
    let isMounted = true;

    async function loadStats() {
      setIsStatsLoading(true);
      setStatsError(null);
      try {
        const [bankroll, summary, pending] = await Promise.all([
          fetchAgentDashboardBankrollWidget(),
          fetchAgentDashboardBettingSummaryWidget(),
          fetchAgentDashboardPendingBetsWidget(),
        ]);
        if (!isMounted) return;
        setBankrollWidget(bankroll);
        setSummaryWidget(summary);
        setPendingWidget(pending);
      } catch (err) {
        if (!isMounted) return;
        setStatsError(handleServiceError(err, "Failed to load betting stats."));
      } finally {
        if (isMounted) setIsStatsLoading(false);
      }
    }

    void loadStats();
    return () => {
      isMounted = false;
    };
  }, []);

  function syncFiltersInUrl(nextLeagueIds: number[], nextStatusId: number) {
    const params = new URLSearchParams(searchParams.toString());
    params.set("status", String(nextStatusId));
    if (nextLeagueIds.length > 0) {
      params.set("leagues", nextLeagueIds.join(","));
    } else {
      params.delete("leagues");
    }
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }

  function handleToggleLeague(leagueId: number) {
    const nextLeagueIds = selectedLeagueIds.includes(leagueId)
      ? selectedLeagueIds.filter((id) => id !== leagueId)
      : [...selectedLeagueIds, leagueId];
    syncFiltersInUrl(nextLeagueIds, selectedStatusId);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleSelectStatus(statusId: number) {
    syncFiltersInUrl(selectedLeagueIds, statusId);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Matches
        </h1>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[280px_minmax(0,1fr)_320px] lg:items-start">
          <aside className="flex flex-col gap-4 self-start lg:sticky lg:top-20">
            <h3 className="px-1 text-sm font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Leagues
            </h3>
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
                className="grid grid-cols-1 gap-0"
              />
            )}
            <div className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
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
          </aside>
          <section className="min-w-0">
            {isLoading ? (
              <MatchesFallback />
            ) : error ? (
              <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">
                {error}
              </p>
            ) : (
              <MatchList matches={matches} />
            )}
          </section>
          <aside className="flex flex-col gap-3 self-start lg:sticky lg:top-20">
            <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Research betting
            </h2>
            {statsError ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {statsError}
              </p>
            ) : null}
            <StatCard
              label="Settled slips"
              value={
                summaryWidget ? String(summaryWidget.settledSlipsCount) : "—"
              }
              helper={
                isStatsLoading && !summaryWidget
                  ? "Loading..."
                  : summaryWidget
                  ? `${summaryWidget.settledSelectionsCount} selections`
                  : undefined
              }
            />
            <StatCard
              label="Win / loss rate"
              value={
                summaryWidget
                  ? `${summaryWidget.winRatePercent.toFixed(1)}% / ${summaryWidget.lossRatePercent.toFixed(1)}%`
                  : "—"
              }
              helper={
                summaryWidget
                  ? `Won ${summaryWidget.wonSlipsCount} | Lost ${summaryWidget.lostSlipsCount}`
                  : isStatsLoading
                  ? "Loading..."
                  : undefined
              }
            />
          </aside>
        </div>
      </main>
    </div>
  );
}
