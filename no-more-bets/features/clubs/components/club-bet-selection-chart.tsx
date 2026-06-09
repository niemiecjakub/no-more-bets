"use client";

import { Pie, PieChart } from "recharts";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import type { ClubBetSelectionStats } from "../interfaces";

const CHART_CONFIG = {
  won: { label: "Won", color: "#22c55e" },
  lost: { label: "Lost", color: "#ef4444" },
} satisfies ChartConfig;

interface ClubBetSelectionChartProps {
  stats: ClubBetSelectionStats;
}

export function ClubBetSelectionChart({ stats }: ClubBetSelectionChartProps) {
  if (stats.totalCount === 0) {
    return (
      <p className="text-sm text-zinc-500 dark:text-zinc-400">
        No settled research bet selections for this club&apos;s matches yet.
      </p>
    );
  }

  const wonPct = ((stats.wonCount / stats.totalCount) * 100).toFixed(1);
  const lostPct = ((stats.lostCount / stats.totalCount) * 100).toFixed(1);

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-center sm:justify-center sm:gap-8">
      <ChartContainer config={CHART_CONFIG} className="mx-auto aspect-square h-[180px] w-[180px]">
        <PieChart>
          <ChartTooltip content={<ChartTooltipContent nameKey="result" />} />
          <Pie
            data={[
              { result: "won", value: stats.wonCount, fill: "var(--color-won)" },
              { result: "lost", value: stats.lostCount, fill: "var(--color-lost)" },
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
      <ul className="space-y-2 text-sm">
        <li className="flex items-center gap-2">
          <span className="h-2.5 w-2.5 rounded-full bg-emerald-500" />
          <span className="text-foreground">
            Won: <span className="font-semibold tabular-nums">{stats.wonCount}</span> ({wonPct}%)
          </span>
        </li>
        <li className="flex items-center gap-2">
          <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
          <span className="text-foreground">
            Lost: <span className="font-semibold tabular-nums">{stats.lostCount}</span> ({lostPct}%)
          </span>
        </li>
      </ul>
    </div>
  );
}
