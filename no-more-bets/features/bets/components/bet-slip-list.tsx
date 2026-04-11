"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import type { BetSelectionItem, BetSlipListItem } from "../interfaces";
import { BET_STATUS } from "../interfaces";
import { formatMatchDate } from "../../../utils/format-date";
import { LazyAgentSessionTranscript } from "./lazy-agent-session-transcript";

interface BetSlipListProps {
  betSlips: BetSlipListItem[];
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

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pl-PL", {
    style: "currency",
    currency: "PLN",
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function SelectionRow({ selection }: { selection: BetSelectionItem }) {
  return (
    <li className="border-t border-zinc-100 py-2 first:border-t-0 first:pt-0 last:pb-0 dark:border-zinc-800/80">
      <Link
        href={`/match/${selection.matchId}`}
        className="-mx-1 block rounded-md px-1 py-0.5 transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900/50"
      >
        <div className="text-sm font-medium text-foreground">
          {selection.homeClubName}
          <span className="mx-1.5 text-zinc-500 dark:text-zinc-400">vs</span>
          {selection.awayClubName}
        </div>
        <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 text-xs text-zinc-600 dark:text-zinc-400">
          <span>{selection.eventTypeName}</span>
          <span className="font-medium text-foreground">{selection.eventOptionName}</span>
          <span className="tabular-nums">@{selection.oddsAtPlacement.toFixed(2)}</span>
        </div>
      </Link>
    </li>
  );
}

function BetSlipCard({ slip }: { slip: BetSlipListItem }) {
  return (
    <li className="overflow-hidden rounded-lg border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800/80">
        <div className="flex flex-wrap items-center gap-2">
          <time
            dateTime={slip.createdAt}
            className="tabular-nums text-sm text-zinc-600 dark:text-zinc-400"
          >
            {formatMatchDate(slip.createdAt)}
          </time>
          <span
            className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(slip.statusId)}`}
          >
            {slip.statusName}
          </span>
        </div>
      </div>
      <div className="grid grid-cols-1 gap-3 border-b border-zinc-100 px-4 py-3 text-sm sm:grid-cols-3 dark:border-zinc-800/80">
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
  slipCount,
}: {
  agentSessionId: number;
  slipCount: number;
}) {
  const [transcriptOpen, setTranscriptOpen] = useState(false);

  return (
    <div className="border-b border-zinc-200 bg-zinc-100/80 dark:border-zinc-800 dark:bg-zinc-900/50">
      <div className="px-4 py-2">
        <p className="text-sm font-medium text-foreground">
          Betting session #{agentSessionId}
          <span className="ml-2 font-normal text-zinc-500 dark:text-zinc-400">
            · {slipCount} slip{slipCount === 1 ? "" : "s"}
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
          <LazyAgentSessionTranscript sessionId={agentSessionId} active={transcriptOpen} />
        </div>
      </details>
    </div>
  );
}

export function BetSlipList({ betSlips }: BetSlipListProps) {
  const groups = useMemo(() => groupBetSlips(betSlips), [betSlips]);

  if (betSlips.length === 0) {
    return (
      <p className="py-12 text-center text-zinc-500 dark:text-zinc-400">No bet slips yet.</p>
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
                slipCount={group.slips.length}
              />
              <ul className="space-y-4 p-4">
                {group.slips.map((slip) => (
                  <BetSlipCard key={slip.id} slip={slip} />
                ))}
              </ul>
            </div>
          ) : (
            <ul className="space-y-4">
              {group.slips.map((slip) => (
                <BetSlipCard key={slip.id} slip={slip} />
              ))}
            </ul>
          )}
        </li>
      ))}
    </ul>
  );
}
