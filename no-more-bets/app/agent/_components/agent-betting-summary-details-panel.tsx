"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Pie, PieChart } from "recharts";
import type {
  AgentDashboardBettingSummaryDetails,
  BetSlipListItem,
} from "@/features/bets/interfaces";
import {
  fetchAgentDashboardBettingSummaryDetails,
  fetchAgentDashboardBettingSummarySlipsPage,
} from "@/features/bets/services/agent-dashboard-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentBettingSummarySlipsSection } from "./agent-betting-summary-slips-section";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";

const CHART_COLORS = {
  won: "#22c55e",
  lost: "#ef4444",
} as const;

function percent(part: number, total: number) {
  if (total === 0) return 0;
  return (part / total) * 100;
}

function mergeSlips(existing: BetSlipListItem[], incoming: BetSlipListItem[]): BetSlipListItem[] {
  const seen = new Set(existing.map((slip) => slip.id));
  const merged = [...existing];
  for (const slip of incoming) {
    if (seen.has(slip.id)) continue;
    seen.add(slip.id);
    merged.push(slip);
  }
  return merged;
}

function SummaryDonut({
  title,
  won,
  lost,
}: {
  title: string;
  won: number;
  lost: number;
}) {
  const total = won + lost;
  const data = [
    { result: "won", value: won, fill: "var(--color-won)" },
    { result: "lost", value: lost, fill: "var(--color-lost)" },
  ];
  const chartConfig = {
    won: {
      label: "Won",
      color: CHART_COLORS.won,
    },
    lost: {
      label: "Lost",
      color: CHART_COLORS.lost,
    },
  } satisfies ChartConfig;

  return (
    <article className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
      <h4 className="mb-3 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        {title}
      </h4>
      <ChartContainer config={chartConfig} className="h-52 min-h-52 w-full">
        <PieChart>
          <ChartTooltip content={<ChartTooltipContent nameKey="result" />} />
          <Pie
            data={data}
            dataKey="value"
            nameKey="result"
            innerRadius={52}
            outerRadius={76}
            paddingAngle={2}
            strokeWidth={0}
          />
        </PieChart>
      </ChartContainer>
      <div className="mt-2 flex items-center justify-between text-sm">
        <span className="text-zinc-600 dark:text-zinc-300">
          Won: <strong>{percent(won, total).toFixed(1)}%</strong> ({won})
        </span>
        <span className="text-zinc-600 dark:text-zinc-300">
          Lost: <strong>{percent(lost, total).toFixed(1)}%</strong> ({lost})
        </span>
      </div>
    </article>
  );
}

export function AgentBettingSummaryDetailsPanel({
  selectedSeasonYears,
}: {
  selectedSeasonYears: string[];
}) {
  const [summary, setSummary] = useState<AgentDashboardBettingSummaryDetails | null>(null);
  const [slips, setSlips] = useState<BetSlipListItem[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<{ at: string; id: number } | null>(null);

  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isSlipsLoading, setIsSlipsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const [error, setError] = useState<string | null>(null);
  const [loadMoreError, setLoadMoreError] = useState<string | null>(null);

  const isLoadingMoreRef = useRef(false);
  const seasonYearsKey = selectedSeasonYears.join(",");

  const applySlipsPage = useCallback(
    (page: Awaited<ReturnType<typeof fetchAgentDashboardBettingSummarySlipsPage>>, append: boolean) => {
      setSlips((current) => (append ? mergeSlips(current, page.items) : page.items));
      setHasMore(page.hasMore);
      setNextCursor(
        page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
          ? { at: page.nextCursorAt, id: page.nextCursorId }
          : null,
      );
    },
    [],
  );

  useEffect(() => {
    let cancelled = false;

    setIsInitialLoading(true);
    setIsSlipsLoading(true);
    setError(null);
    setLoadMoreError(null);

    Promise.all([
      fetchAgentDashboardBettingSummaryDetails(selectedSeasonYears),
      fetchAgentDashboardBettingSummarySlipsPage({ seasonYears: selectedSeasonYears }),
    ])
      .then(([summaryData, slipsPage]) => {
        if (cancelled) return;
        setSummary(summaryData);
        applySlipsPage(slipsPage, false);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setError(handleServiceError(caughtError, "Failed to load betting summary details."));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsInitialLoading(false);
          setIsSlipsLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [applySlipsPage, seasonYearsKey, selectedSeasonYears]);

  const loadMore = useCallback(() => {
    if (!hasMore || !nextCursor || isLoadingMoreRef.current) return;

    isLoadingMoreRef.current = true;
    setIsLoadingMore(true);
    setLoadMoreError(null);

    fetchAgentDashboardBettingSummarySlipsPage({
      afterCreatedAt: nextCursor.at,
      afterId: nextCursor.id,
      seasonYears: selectedSeasonYears,
    })
      .then((page) => {
        applySlipsPage(page, true);
      })
      .catch((caughtError) => {
        setLoadMoreError(handleServiceError(caughtError, "Failed to load more betting slips."));
      })
      .finally(() => {
        isLoadingMoreRef.current = false;
        setIsLoadingMore(false);
      });
  }, [applySlipsPage, hasMore, nextCursor, selectedSeasonYears]);

  if (isInitialLoading) {
    return <div className="h-72 animate-pulse rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950" />;
  }

  if (error) {
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {error}
      </p>
    );
  }

  if (!summary) {
    return null;
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="grid gap-4 md:grid-cols-2">
        <SummaryDonut
          title="Won/Lost Slips"
          won={summary.wonSlipsCount}
          lost={summary.lostSlipsCount}
        />
        <SummaryDonut
          title="Won/Lost Selections"
          won={summary.wonSelectionsCount}
          lost={summary.lostSelectionsCount}
        />
      </div>

      <section>
        <h4 className="mb-3 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Settled Betting Slips
        </h4>
        <AgentBettingSummarySlipsSection
          slips={slips}
          isLoading={isSlipsLoading}
          hasMore={hasMore}
          isLoadingMore={isLoadingMore}
          onLoadMore={loadMore}
          loadMoreError={loadMoreError}
          onRetryLoadMore={loadMore}
        />
      </section>
    </section>
  );
}
