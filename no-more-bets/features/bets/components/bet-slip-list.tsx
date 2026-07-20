"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { ChevronRight } from "lucide-react";
import type { BetSelectionItem, BetSlipListItem } from "../interfaces";
import { BET_STATUS } from "../interfaces";
import { SlugIcon } from "@/components/slug-icon";
import { formatCurrency } from "@/utils/format-currency";
import { clubLogoSlugSegment } from "@/utils/club-logo-slug";
import { formatMatchDate } from "../../../utils/format-date";
import { LazyAgentSessionTranscript } from "./lazy-agent-session-transcript";

interface BetSlipListProps {
  betSlips: BetSlipListItem[];
  groupBySession?: boolean;
  showSessionLink?: boolean;
}

interface BetSlipGroupModel {
  key: string;
  agentSessionId: number | null;
  slips: BetSlipListItem[];
  maxCreatedMs: number;
}

function getStatusBadgeClass(statusId: number): string {
  switch (statusId) {
    case BET_STATUS.Pending:
      return "bg-amber-100 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30";
    case BET_STATUS.Won:
      return "bg-emerald-100 text-emerald-800 ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30";
    case BET_STATUS.Lost:
      return "bg-red-100 text-red-800 ring-red-600/20 dark:bg-red-900/40 dark:text-red-400 dark:ring-red-500/30";
    default:
      return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
  }
}

function SelectionRow({ selection }: { selection: BetSelectionItem }) {
  const homeLogoSlug = clubLogoSlugSegment(
    selection.homeClubSlug,
    selection.homeClubName
  );
  const awayLogoSlug = clubLogoSlugSegment(
    selection.awayClubSlug,
    selection.awayClubName
  );

  return (
    <li className="border-t border-zinc-100 py-2 first:border-t-0 first:pt-0 last:pb-0 dark:border-zinc-800/80">
      <Link
        href={`/match/${selection.matchId}`}
        className="-mx-1 flex flex-col gap-2 rounded-md px-1 py-0.5 text-left transition-colors hover:bg-zinc-50 sm:flex-row sm:items-center sm:gap-4 dark:hover:bg-zinc-900/50"
      >
        <div className="min-w-0 sm:min-w-48 sm:flex-1">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-medium text-foreground">
            <span className="min-w-0 truncate">{selection.homeClubName}</span>
            <SlugIcon
              kind="club"
              slug={homeLogoSlug}
              alt={selection.homeClubName}
              className="h-5 w-5"
            />
            <span className="shrink-0 text-zinc-500 dark:text-zinc-400">vs</span>
            <SlugIcon
              kind="club"
              slug={awayLogoSlug}
              alt={selection.awayClubName}
              className="h-5 w-5"
            />
            <span className="min-w-0 truncate">{selection.awayClubName}</span>
          </div>
        </div>
        <div className="flex min-w-0 w-full flex-wrap items-center justify-start gap-x-3 gap-y-1 text-xs text-zinc-600 dark:text-zinc-400 sm:flex-1 sm:justify-end">
          <div className="flex min-w-0 flex-wrap items-center gap-x-3 gap-y-0.5">
            <span>{selection.eventTypeName}</span>
            <span className="font-medium text-foreground">{selection.eventOptionName}</span>
            <span className="tabular-nums">@{selection.oddsAtPlacement.toFixed(2)}</span>
          </div>
          <span
            className={`inline-flex shrink-0 items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(selection.statusId)}`}
          >
            {selection.statusName}
          </span>
        </div>
      </Link>
    </li>
  );
}

function BetSlipCard({
  slip,
  stackInSession,
  showSessionLink = true,
}: {
  slip: BetSlipListItem;
  stackInSession?: { index: number; total: number };
  showSessionLink?: boolean;
}) {
  const stackClass =
    stackInSession != null
      ? [
          stackInSession.index > 0 && "-mt-px relative z-[1]",
          "rounded-none",
        ]
          .filter(Boolean)
          .join(" ")
      : "rounded-lg";

  return (
    <li
      className={`overflow-hidden border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950 ${stackClass}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800/80">
        <div className="flex flex-wrap items-center gap-2">
          <span
            className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(slip.statusId)}`}
          >
            {slip.statusName}
          </span>
          <time
            dateTime={slip.createdAt}
            className="tabular-nums text-sm text-zinc-600 dark:text-zinc-400"
            title="Bet placement time"
          >
            Placed: {formatMatchDate(slip.createdAt)}
          </time>
        </div>
        {showSessionLink && slip.agentSessionId != null ? (
          <Link
            href={`/agent?widget=sessions&sessionId=${slip.agentSessionId}`}
            className="inline-flex shrink-0 items-center gap-1 rounded-md border border-zinc-300 bg-zinc-700 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-zinc-800 dark:border-zinc-600 dark:bg-zinc-700 dark:hover:bg-zinc-600"
          >
            Session #{slip.agentSessionId}
            <ChevronRight className="h-3.5 w-3.5 text-white/90" aria-hidden />
          </Link>
        ) : null}
      </div>
      <div className="grid grid-cols-3 gap-3 border-b border-zinc-100 px-4 py-3 text-sm dark:border-zinc-800/80">
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Stake</span>
          <p className="font-semibold tabular-nums text-foreground">
            {formatCurrency(slip.stakeAmount)}
          </p>
        </div>
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Combined odds</span>
          <p className="font-semibold tabular-nums text-foreground">{slip.totalOdds.toFixed(2)}</p>
        </div>
        <div>
          <span className="text-zinc-500 dark:text-zinc-400">Potential payout</span>
          <p className="font-semibold tabular-nums text-foreground">
            {formatCurrency(slip.potentialPayout)}
          </p>
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
          <SelectionRow key={`${slip.id}-${sel.matchId}-${idx}`} selection={sel} />
        ))}
      </ul>
    </li>
  );
}

function groupBetSlips(slips: BetSlipListItem[]): BetSlipGroupModel[] {
  const map = new Map<string, BetSlipListItem[]>();
  for (const slip of slips) {
    const key =
      slip.agentSessionId != null ? `session-${slip.agentSessionId}` : `orphan-${slip.id}`;
    const list = map.get(key) ?? [];
    list.push(slip);
    map.set(key, list);
  }
  const groups: BetSlipGroupModel[] = [];
  for (const [key, list] of map) {
    const agentSessionId = list[0]?.agentSessionId ?? null;
    const maxCreatedMs = Math.max(...list.map((s) => new Date(s.createdAt).getTime()));
    groups.push({ key, agentSessionId, slips: list, maxCreatedMs });
  }
  groups.sort((a, b) => b.maxCreatedMs - a.maxCreatedMs);
  return groups;
}

function BetSessionGroupHeader({
  agentSessionId,
  slips,
}: {
  agentSessionId: number;
  slips: BetSlipListItem[];
}) {
  const [transcriptOpen, setTranscriptOpen] = useState(false);

  return (
    <div className="border-b border-zinc-200 bg-zinc-100/80 dark:border-zinc-800 dark:bg-zinc-900/50">
      <div className="px-4 py-2">
        <p className="text-sm font-medium text-foreground">
          Betting session #{agentSessionId}
          <span className="ml-2 font-normal text-zinc-500 dark:text-zinc-400">
            · {slips.length} slip{slips.length === 1 ? "" : "s"}
          </span>
        </p>
      </div>
      <details
        className="group border-t border-zinc-200 dark:border-zinc-800"
        onToggle={(e) => {
          const el = e.currentTarget;
          if (el.open) setTranscriptOpen(true);
        }}
      >
        <summary className="cursor-pointer list-none px-4 py-2 text-sm text-zinc-600 hover:bg-zinc-200/60 dark:text-zinc-400 dark:hover:bg-zinc-800/60">
          <span className="inline-flex w-full items-center justify-between gap-2">
            <span>Session transcript</span>
            <span className="text-xs transition-transform group-open:rotate-180">▼</span>
          </span>
        </summary>
        <div className="border-t border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
          <LazyAgentSessionTranscript
            sessionId={agentSessionId}
            active={transcriptOpen}
          />
        </div>
      </details>
    </div>
  );
}

export function BetSlipList({
  betSlips,
  groupBySession = true,
  showSessionLink = true,
}: BetSlipListProps) {
  const groups = useMemo(
    () => (groupBySession ? groupBetSlips(betSlips) : []),
    [betSlips, groupBySession]
  );

  if (betSlips.length === 0) {
    return (
      <p className="py-12 text-center text-zinc-500 dark:text-zinc-400">No bet slips yet.</p>
    );
  }

  if (!groupBySession) {
    return (
      <ul className="space-y-3">
        {betSlips.map((slip) => (
          <BetSlipCard key={slip.id} slip={slip} showSessionLink={showSessionLink} />
        ))}
      </ul>
    );
  }

  return (
    <ul className="space-y-6">
      {groups.map((group) => (
        <li key={group.key}>
          {group.agentSessionId != null ? (
            <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
              <BetSessionGroupHeader
                agentSessionId={group.agentSessionId}
                slips={group.slips}
              />
              <ul>
                {group.slips.map((slip, index) => (
                  <BetSlipCard
                    key={slip.id}
                    slip={slip}
                    stackInSession={{ index, total: group.slips.length }}
                    showSessionLink={showSessionLink}
                  />
                ))}
              </ul>
            </div>
          ) : (
            <ul>
              {group.slips.map((slip) => (
                <BetSlipCard key={slip.id} slip={slip} showSessionLink={showSessionLink} />
              ))}
            </ul>
          )}
        </li>
      ))}
    </ul>
  );
}
