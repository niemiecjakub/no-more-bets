"use client";

import { Pie, PieChart } from "recharts";
import type { AgentDashboardResearchBettingSummaryWidget } from "../interfaces";
import { ResearchScenarioPnlWidget } from "./research-scenario-pnl-widget";
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";

const SUMMARY_CHART_CONFIG = {
  won: { label: "Won", color: "#22c55e" },
  lost: { label: "Lost", color: "#ef4444" },
} satisfies ChartConfig;

export interface ResearchBettingPanelProps {
  summaryWidget: AgentDashboardResearchBettingSummaryWidget | null;
  statsError: string | null;
  scopeLabel: string;
  showTitle?: boolean;
}

export function ResearchBettingPanel({
  summaryWidget,
  statsError,
  scopeLabel,
  showTitle = true,
}: ResearchBettingPanelProps) {
  return (
    <div className="flex flex-col gap-3">
      {showTitle ? (
        <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Research betting
        </h2>
      ) : null}
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
              {summaryWidget.winRatePercent.toFixed(1)}% / {summaryWidget.lossRatePercent.toFixed(1)}%
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
          <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">Scope: {scopeLabel}</p>
        </article>
      ) : null}
      {summaryWidget ? <ResearchScenarioPnlWidget summary={summaryWidget} scopeLabel={scopeLabel} /> : null}
    </div>
  );
}
