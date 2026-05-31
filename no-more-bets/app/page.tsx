"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Pie, PieChart } from "recharts";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { MatchList } from "../features/matches/components/match-list";
import {
  ALL_STATUSES_ID,
  MatchFiltersPanel,
  statusFilters,
} from "../features/matches/components/match-filters-panel";
import { MatchFiltersMobileSheet } from "../features/matches/components/match-filters-mobile-sheet";
import { useMatchStore } from "@/store/match-store";
import { useLeagueStore } from "@/store/league-store";
import { fetchAgentDashboardResearchBettingSummaryWidget } from "@/features/bets/services/research-dashboard-api";
import type {
  AgentDashboardResearchBettingSummaryWidget,
} from "@/features/bets/interfaces";
import { handleServiceError } from "@/lib/error-handler";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";

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

const SUMMARY_CHART_CONFIG = {
  won: { label: "Won", color: "#22c55e" },
  lost: { label: "Lost", color: "#ef4444" },
} satisfies ChartConfig;

export default function Home() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const {
    matches,
    isLoading,
    error,
    hasMore,
    isLoadingMore,
    loadMoreError,
    setMatches,
    loadMoreMatches,
    retryLoadMore,
  } = useMatchStore();

  const handleLoadMoreMatches = useCallback(() => {
    void loadMoreMatches();
  }, [loadMoreMatches]);

  const handleRetryLoadMoreMatches = useCallback(() => {
    retryLoadMore();
  }, [retryLoadMore]);
  const {
    leagues,
    isLoading: isLeaguesLoading,
    error: leaguesError,
    setLeagues,
  } = useLeagueStore();
  const [summaryWidget, setSummaryWidget] =
    useState<AgentDashboardResearchBettingSummaryWidget | null>(null);
  const [isStatsLoading, setIsStatsLoading] = useState(false);
  const [statsError, setStatsError] = useState<string | null>(null);

  const { selectedLeagueIds, selectedStatusId, matchFilters } = useMemo(() => {
    const statusParam = Number(searchParams.get("status"));
    const matchedStatus = statusFilters.find(
      (statusFilter) => statusFilter.id === statusParam
    );
    const parsedStatusId = matchedStatus?.id ?? ALL_STATUSES_ID;

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

  const researchStatsScopeLabel = useMemo(() => {
    if (selectedLeagueIds.length === 0) return "All leagues";

    const selectedNames = leagues
      .filter((league) => selectedLeagueIds.includes(league.id))
      .map((league) => league.name);

    if (selectedNames.length === 0) return "Selected leagues";
    return selectedNames.join(", ");
  }, [leagues, selectedLeagueIds]);

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
        const summary = await fetchAgentDashboardResearchBettingSummaryWidget(selectedLeagueIds);
        if (!isMounted) return;
        setSummaryWidget(summary);
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
  }, [selectedLeagueIds]);

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

  const filterPanelProps = {
    leagues,
    isLeaguesLoading,
    leaguesError,
    selectedLeagueIds,
    selectedStatusId,
    onToggleLeague: handleToggleLeague,
    onSelectStatus: handleSelectStatus,
  };

  return (
      <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,2.75fr)_minmax(0,1fr)] lg:items-start">
          <div className="order-1 lg:hidden">
            <MatchFiltersMobileSheet {...filterPanelProps} />
          </div>
          <aside className="order-1 hidden flex-col gap-4 self-start lg:sticky lg:top-20 lg:flex">
            <MatchFiltersPanel {...filterPanelProps} />
          </aside>
          <section className="order-3 min-w-0 lg:order-2">
            {isLoading ? (
              <MatchesFallback />
            ) : error ? (
              <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">
                {error}
              </p>
            ) : (
              <MatchList
                matches={matches}
                hasMore={hasMore}
                isLoadingMore={isLoadingMore}
                onLoadMore={handleLoadMoreMatches}
                loadMoreError={loadMoreError}
                onRetryLoadMore={handleRetryLoadMoreMatches}
              />
            )}
          </section>
          <aside className="order-2 flex flex-col gap-3 self-start lg:order-3 lg:sticky lg:top-20">
            <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Research betting
            </h2>
            <p className="px-1 text-xs text-zinc-500 dark:text-zinc-400">
              Scope: {researchStatsScopeLabel}
            </p>
            {statsError ? (
              <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {statsError}
              </p>
            ) : null}
            {summaryWidget ? (
              <article className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
                  Won vs lost selections
                </p>
                <div className="mb-2">
                  <p className="text-base font-semibold tabular-nums tracking-tight text-foreground">
                    {summaryWidget.winRatePercent.toFixed(1)}% /{" "}
                    {summaryWidget.lossRatePercent.toFixed(1)}%
                  </p>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400">
                    Won {summaryWidget.wonSelectionsCount} | Lost {summaryWidget.lostSelectionsCount}
                  </p>
                </div>
                <ChartContainer config={SUMMARY_CHART_CONFIG} className="h-40 w-full">
                  <PieChart>
                    <ChartTooltip content={<ChartTooltipContent nameKey="result" />} />
                    <Pie
                      data={[
                        { result: "won", value: summaryWidget.wonSelectionsCount, fill: "var(--color-won)" },
                        { result: "lost", value: summaryWidget.lostSelectionsCount, fill: "var(--color-lost)" },
                      ]}
                      dataKey="value"
                      nameKey="result"
                      innerRadius={38}
                      outerRadius={58}
                      strokeWidth={0}
                      paddingAngle={2}
                    />
                  </PieChart>
                </ChartContainer>
              </article>
            ) : null}
          </aside>
        </div>
      </main>
  );
}
