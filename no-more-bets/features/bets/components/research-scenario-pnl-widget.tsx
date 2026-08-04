"use client";

import { CircleHelp } from "lucide-react";
import { Bar, BarChart, Cell, XAxis, YAxis } from "recharts";
import type { AgentDashboardResearchBettingSummaryWidget } from "../interfaces";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";

const PNL_CHART_CONFIG = {
  roi: { label: "ROI" },
  parlay: { label: "Parlay", color: "#6366f1" },
  singles: { label: "Singles", color: "#0d9488" },
} satisfies ChartConfig;

function roiClass(roi: number): string {
  if (roi > 0) return "text-emerald-700 dark:text-emerald-400";
  if (roi < 0) return "text-red-700 dark:text-red-400";
  return "text-foreground";
}

function formatRoi(roi: number): string {
  const pct = roi * 100;
  const body = `${Math.abs(pct).toFixed(1)}%`;
  if (pct > 0) return `+${body}`;
  if (pct < 0) return `−${body}`;
  return body;
}

function ScenarioRow({ title, roi }: { title: string; roi: number }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <p className="min-w-0 text-sm font-medium text-foreground">{title}</p>
      <p className={`shrink-0 text-sm font-semibold tabular-nums ${roiClass(roi)}`}>
        {formatRoi(roi)}
      </p>
    </div>
  );
}

function MethodologyHint() {
  return (
    <Tooltip>
      <TooltipTrigger
        type="button"
        className="inline-flex shrink-0 items-center gap-1 rounded-sm text-xs font-medium normal-case tracking-normal text-zinc-500 underline-offset-2 hover:text-zinc-700 hover:underline dark:text-zinc-400 dark:hover:text-zinc-200"
        aria-label="How hypothetical profit and loss is calculated"
      >
        <CircleHelp className="size-3.5" aria-hidden />
        How it works
      </TooltipTrigger>
      <TooltipContent
        side="top"
        align="end"
        className="max-w-60 flex-col items-start gap-2 px-3 py-2.5 text-left font-normal normal-case tracking-normal"
      >
        <p>
          Paper comparison of each settled research slip under two equal-risk
          scenarios. No real bankroll is used.
        </p>
        <p>
          <span className="font-medium">Parlay:</span> one bet with stake equal
          to one unit × legs. All legs must win; otherwise the full stake is
          lost.
        </p>
        <p>
          <span className="font-medium">Singles:</span> each leg is a separate
          one-unit bet. Wins and losses settle independently.
        </p>
        <p>Totals sum those outcomes across slips in the current scope.</p>
      </TooltipContent>
    </Tooltip>
  );
}

function WidgetHeading() {
  return (
    <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
      Hypothetical Profit & Loss
    </p>
  );
}

export function ResearchScenarioPnlWidget({
  summary,
  scopeLabel,
}: {
  summary: AgentDashboardResearchBettingSummaryWidget;
  scopeLabel: string;
}) {
  if (summary.scenarioSlipCount === 0) {
    return (
      <article className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
        <WidgetHeading />
        <p className="text-sm text-zinc-500 dark:text-zinc-400">
          No settled research slips
        </p>
        <div className="mt-2">
          <MethodologyHint />
        </div>
        <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">
          Scope: {scopeLabel}
        </p>
      </article>
    );
  }

  const chartData = [
    {
      scenario: "parlay",
      label: "Parlay",
      roi: summary.parlay.roi * 100,
      fill: summary.parlay.roi >= 0 ? "#22c55e" : "#ef4444",
    },
    {
      scenario: "singles",
      label: "Singles",
      roi: summary.singles.roi * 100,
      fill: summary.singles.roi >= 0 ? "#22c55e" : "#ef4444",
    },
  ];

  return (
    <article className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
      <WidgetHeading />
      <p className="mb-2 text-xs text-zinc-500 dark:text-zinc-400">
        {summary.scenarioSlipCount} slip
        {summary.scenarioSlipCount === 1 ? "" : "s"}
      </p>
      <div className="mb-3">
        <MethodologyHint />
      </div>
      <div className="space-y-2.5">
        <ScenarioRow title="Parlay" roi={summary.parlay.roi} />
        <ScenarioRow title="Singles" roi={summary.singles.roi} />
      </div>
      <ChartContainer config={PNL_CHART_CONFIG} className="mt-3 h-36 w-full aspect-auto">
        <BarChart data={chartData} margin={{ top: 4, right: 4, left: 4, bottom: 0 }}>
          <XAxis
            dataKey="label"
            tickLine={false}
            axisLine={false}
            tick={{ fontSize: 11 }}
          />
          <YAxis
            tickLine={false}
            axisLine={false}
            width={44}
            tick={{ fontSize: 10 }}
            tickFormatter={(v: number) => `${Math.round(v)}%`}
          />
          <ChartTooltip
            content={
              <ChartTooltipContent
                nameKey="scenario"
                formatter={(value) => formatRoi(Number(value) / 100)}
              />
            }
          />
          <Bar dataKey="roi" radius={[4, 4, 0, 0]} maxBarSize={48}>
            {chartData.map((entry) => (
              <Cell key={entry.scenario} fill={entry.fill} />
            ))}
          </Bar>
        </BarChart>
      </ChartContainer>
      <p className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">
        Scope: {scopeLabel}
      </p>
    </article>
  );
}
