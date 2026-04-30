"use client";

import { useEffect, useMemo, useState } from "react";
import { Pie, PieChart } from "recharts";
import type { AgentDashboardBettingSummaryDetails } from "@/features/bets/interfaces";
import { fetchAgentDashboardBettingSummaryDetails } from "@/features/bets/services/agent-dashboard-api";
import { handleServiceError } from "@/lib/error-handler";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";
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

export function AgentBettingSummaryDetailsPanel() {
  const [details, setDetails] = useState<AgentDashboardBettingSummaryDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    fetchAgentDashboardBettingSummaryDetails()
      .then((data) => {
        if (!cancelled) setDetails(data);
      })
      .catch((caughtError) => {
        if (!cancelled) setError(handleServiceError(caughtError, "Failed to load betting summary details."));
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const slips = useMemo(() => details?.slips ?? [], [details]);

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

  if (!details) {
    return null;
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="grid gap-4 md:grid-cols-2">
        <SummaryDonut
          title="Won/Lost Slips"
          won={details.wonSlipsCount}
          lost={details.lostSlipsCount}
        />
        <SummaryDonut
          title="Won/Lost Selections"
          won={details.wonSelectionsCount}
          lost={details.lostSelectionsCount}
        />
      </div>

      <section>
        <h4 className="mb-3 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Settled Betting Slips
        </h4>
        <BetSlipList betSlips={slips} groupBySession={false} />
      </section>
    </section>
  );
}
