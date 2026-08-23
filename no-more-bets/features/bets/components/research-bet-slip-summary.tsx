"use client";

import Link from "next/link";
import type {
  BetSelectionSummaryDto,
  BetSlipSummaryDto,
  ResearchBetScenarioStatsDto,
} from "../interfaces";
import { BET_STATUS } from "../interfaces";
import { SlugIcon } from "@/components/slug-icon";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { formatCurrency } from "@/utils/format-currency";
import { clubLogoSlugSegment } from "@/utils/club-logo-slug";
import { formatMatchDate } from "../../../utils/format-date";
import { matchPath } from "@/lib/paths";

function getStatusBadgeClass(status: BetSlipSummaryDto["status"]): string {
  switch (status) {
    case BET_STATUS.Pending:
      return "bg-amber-100 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30";
    case BET_STATUS.Won:
      return "bg-emerald-100 text-emerald-800 ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30";
    case BET_STATUS.Lost:
      return "bg-red-100 text-red-800 ring-red-600/20 dark:bg-red-900/40 dark:text-red-400 dark:ring-red-500/30";
    case BET_STATUS.Canceled:
      return "bg-zinc-200 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
    default:
      return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
  }
}

function betStatusLabel(status: BetSlipSummaryDto["status"]): string {
  switch (status) {
    case BET_STATUS.Pending:
      return "Pending";
    case BET_STATUS.Won:
      return "Won";
    case BET_STATUS.Lost:
      return "Lost";
    case BET_STATUS.Canceled:
      return "Canceled";
    default:
      return "Unknown";
  }
}

function roiClass(roi: number): string {
  if (roi > 0) return "text-emerald-700 dark:text-emerald-400";
  if (roi < 0) return "text-red-700 dark:text-red-400";
  return "text-foreground";
}

function formatRoi(roi: number | null): string {
  if (roi == null) return "Pending";
  const pct = roi * 100;
  const body = `${Math.abs(pct).toFixed(1)}%`;
  if (pct > 0) return `+${body}`;
  if (pct < 0) return `−${body}`;
  return body;
}

function scenarioRoi(profit: number | null, stakeTotal: number): number | null {
  if (profit == null || stakeTotal <= 0) return null;
  return profit / stakeTotal;
}

function SelectionRowMatchPage({ selection }: { selection: BetSelectionSummaryDto }) {
  return (
    <li className="flex flex-wrap items-center justify-between gap-2 border-t border-zinc-100 py-2.5 text-sm first:border-t-0 first:pt-0 dark:border-zinc-800/80">
      <div className="flex min-w-0 flex-wrap items-center gap-x-2 gap-y-1 text-zinc-600 dark:text-zinc-400">
        <span>{selection.eventTypeName}</span>
        <span className="font-medium text-foreground">{selection.outcomeKey}</span>
        <span className="tabular-nums text-zinc-500 dark:text-zinc-400">@{selection.oddsAtPlacement.toFixed(2)}</span>
      </div>
      <span
        className={`inline-flex shrink-0 items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(selection.status)}`}
      >
        {betStatusLabel(selection.status)}
      </span>
    </li>
  );
}

function SelectionRowDefault({ selection }: { selection: BetSelectionSummaryDto }) {
  const homeLogoSlug = clubLogoSlugSegment(undefined, selection.homeClubName);
  const awayLogoSlug = clubLogoSlugSegment(undefined, selection.awayClubName);

  return (
    <li className="border-t border-zinc-100 py-2 first:border-t-0 first:pt-0 last:pb-0 dark:border-zinc-800/80">
      <Link
        href={matchPath({ id: selection.matchId })}
        className="-mx-1 flex items-center gap-3 rounded-md px-1 py-0.5 text-left transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900/50"
      >
        <div className="flex min-w-0 flex-1 flex-col gap-1 sm:flex-row sm:items-center sm:gap-4">
          <div className="min-w-0 sm:min-w-48 sm:flex-1">
            <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-medium text-foreground">
              <span className="min-w-0 truncate">{selection.homeClubName}</span>
              <SlugIcon kind="club" slug={homeLogoSlug} alt={selection.homeClubName} className="h-5 w-5" />
              <span className="shrink-0 text-zinc-500 dark:text-zinc-400">vs</span>
              <SlugIcon kind="club" slug={awayLogoSlug} alt={selection.awayClubName} className="h-5 w-5" />
              <span className="min-w-0 truncate">{selection.awayClubName}</span>
            </div>
          </div>
          <div className="flex min-w-0 flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-zinc-600 dark:text-zinc-400 sm:flex-1 sm:justify-end">
            <span>{selection.eventTypeName}</span>
            <span className="font-medium text-foreground">{selection.outcomeKey}</span>
          </div>
        </div>
        <span
          className={`inline-flex shrink-0 items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(selection.status)}`}
        >
          {betStatusLabel(selection.status)}
        </span>
      </Link>
    </li>
  );
}

function formatCombinedOddsTooltip(legOdds: number[], combinedOdds: number): string {
  if (legOdds.length === 0) {
    return `Combined odds ${combinedOdds.toFixed(2)}`;
  }
  if (legOdds.length === 1) {
    return `Single-leg parlay: ${legOdds[0].toFixed(2)}`;
  }
  const factors = legOdds.map((o) => o.toFixed(2)).join(" × ");
  return `${factors} = ${combinedOdds.toFixed(2)}`;
}

function ScenarioCard({
  title,
  description,
  combinedOdds,
  legOdds,
  roi,
}: {
  title: string;
  description: string;
  combinedOdds?: number;
  legOdds?: number[];
  roi: number | null;
}) {
  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 rounded-md border border-zinc-200 px-3 py-2 text-sm dark:border-zinc-800">
      <Tooltip>
        <TooltipTrigger className="cursor-help border-0 bg-transparent p-0 text-left">
          <span className="font-medium text-foreground underline decoration-dotted underline-offset-2">
            {title}
          </span>
        </TooltipTrigger>
        <TooltipContent side="top">{description}</TooltipContent>
      </Tooltip>
      {combinedOdds != null ? (
        <Tooltip>
          <TooltipTrigger className="cursor-help border-0 bg-transparent p-0 text-left">
            <span className="tabular-nums text-zinc-600 underline decoration-dotted underline-offset-2 dark:text-zinc-400">
              @{combinedOdds.toFixed(2)}
            </span>
          </TooltipTrigger>
          <TooltipContent side="top">
            {formatCombinedOddsTooltip(legOdds ?? [], combinedOdds)}
          </TooltipContent>
        </Tooltip>
      ) : null}
      <span
        className={`ml-auto font-semibold tabular-nums ${roi == null ? "text-zinc-500 dark:text-zinc-400" : roiClass(roi)}`}
      >
        {formatRoi(roi)}
      </span>
    </div>
  );
}

function ScenarioComparison({ scenarios }: { scenarios: ResearchBetScenarioStatsDto }) {
  const legOdds = scenarios.singles.legs.map((leg) => leg.odds);
  return (
    <div className="mt-4 space-y-3">
      <div>
        <h4 className="text-sm font-semibold text-foreground">Hypothetical result</h4>
        <p className="mt-0.5 text-xs text-zinc-500 dark:text-zinc-400">
          Equal-stake parlay vs singles (paper).
        </p>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <ScenarioCard
          title="Parlay"
          description="One slip - all legs must win"
          combinedOdds={scenarios.parlay.combinedOdds}
          legOdds={legOdds}
          roi={scenarioRoi(scenarios.parlay.profit, scenarios.parlay.stakeTotal)}
        />
        <ScenarioCard
          title="Singles"
          description="Each leg as its own bet"
          roi={scenarioRoi(scenarios.singles.profit, scenarios.singles.stakeTotal)}
        />
      </div>
    </div>
  );
}

interface ResearchBetSlipSummaryProps {
  slip: BetSlipSummaryDto | null;
  isLoading: boolean;
  error?: string;
  /** Match page: legs + optional equal-stake scenario P&L. */
  variant?: "default" | "matchPage";
  scenarios?: ResearchBetScenarioStatsDto | null;
}

export function ResearchBetSlipSummary({
  slip,
  isLoading,
  error,
  variant = "default",
  scenarios = null,
}: ResearchBetSlipSummaryProps) {
  if (error) {
    return <p className="text-sm text-red-800 dark:text-red-200">{error}</p>;
  }
  if (isLoading) {
    return <p className="text-sm text-zinc-500 dark:text-zinc-400">Loading research bet slip…</p>;
  }
  if (slip == null) {
    return (
      <p className="text-sm text-zinc-500 dark:text-zinc-400">
        No research bet slip recorded for this match yet.
      </p>
    );
  }

  if (variant === "matchPage") {
    return (
      <div>
        <ul className="flex flex-col">
          {slip.selections.map((sel, idx) => (
            <SelectionRowMatchPage key={`${slip.id}-${sel.matchId}-${idx}`} selection={sel} />
          ))}
        </ul>
        {scenarios != null ? <ScenarioComparison scenarios={scenarios} /> : null}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800/80">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(slip.status)}`}
          >
            {betStatusLabel(slip.status)}
          </span>
          <span className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            Paper / research
          </span>
          <time dateTime={slip.createdAt} className="tabular-nums text-sm text-zinc-600 dark:text-zinc-400">
            {formatMatchDate(slip.createdAt)}
          </time>
        </div>
      </div>
      <div className="grid grid-cols-3 gap-3 border-b border-zinc-100 px-4 py-3 text-sm dark:border-zinc-800/80">
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Stake</span>
          <p className="font-semibold tabular-nums text-foreground">{formatCurrency(slip.stakeAmount)}</p>
        </div>
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Combined odds</span>
          <p className="font-semibold tabular-nums text-foreground">{slip.totalOdds.toFixed(2)}</p>
        </div>
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Potential payout</span>
          <p className="font-semibold tabular-nums text-foreground">{formatCurrency(slip.potentialPayout)}</p>
        </div>
      </div>
      {slip.estimatedWinProbability != null || slip.rationale ? (
        <div className="space-y-2 border-b border-zinc-100 px-4 py-3 text-sm dark:border-zinc-800/80">
          {slip.estimatedWinProbability != null ? (
            <div>
              <span className="text-zinc-500 dark:text-zinc-400">Est. win probability</span>
              <p className="font-semibold tabular-nums text-foreground">
                {(slip.estimatedWinProbability * 100).toFixed(0)}%
              </p>
            </div>
          ) : null}
          {slip.rationale ? (
            <div>
              <span className="text-zinc-500 dark:text-zinc-400">Rationale</span>
              <p className="mt-0.5 whitespace-pre-wrap text-foreground">{slip.rationale}</p>
            </div>
          ) : null}
        </div>
      ) : null}
      <ul className="px-4 py-3">
        {slip.selections.map((sel, idx) => (
          <SelectionRowDefault key={`${slip.id}-${sel.matchId}-${idx}`} selection={sel} />
        ))}
      </ul>
    </div>
  );
}
