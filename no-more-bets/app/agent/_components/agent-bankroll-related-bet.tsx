import type { BankrollEntryBetDetailsDto, BankrollEntryListItemDto } from "@/features/bets/interfaces";
import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { SlugIcon } from "@/components/slug-icon";
import { formatCurrency } from "@/utils/format-currency";
import { clubLogoSlugSegment } from "@/utils/club-logo-slug";
import { formatMatchDate } from "@/utils/format-date";

interface AgentBankrollRelatedBetProps {
  selectedEntry: BankrollEntryListItemDto | null;
  details: BankrollEntryBetDetailsDto | null;
  isLoading: boolean;
  error: string | null;
}

export function AgentBankrollRelatedBet({
  selectedEntry,
  details,
  isLoading,
  error,
}: AgentBankrollRelatedBetProps) {
  function getStatusBadgeClass(statusName: string): string {
    const normalized = statusName.toLowerCase();
    if (normalized === "pending")
      return "bg-amber-100 text-amber-800 ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30";
    if (normalized === "won")
      return "bg-emerald-100 text-emerald-800 ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30";
    if (normalized === "lost")
      return "bg-red-100 text-red-800 ring-red-600/20 dark:bg-red-900/40 dark:text-red-400 dark:ring-red-500/30";
    return "bg-zinc-100 text-zinc-700 ring-zinc-600/20 dark:bg-zinc-800 dark:text-zinc-300 dark:ring-zinc-500/30";
  }

  if (!selectedEntry) {
    return (
      <div className="flex h-full min-h-[min(78vh,44rem)] items-center justify-center p-4 text-sm text-zinc-500 dark:text-zinc-400">
        Select an entry to preview related bet details.
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="h-full min-h-[min(78vh,44rem)] animate-pulse p-3">
        <div className="h-full rounded-lg bg-zinc-100 dark:bg-zinc-900" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="h-full min-h-[min(78vh,44rem)] bg-red-50 p-4 text-sm text-red-800 dark:bg-red-950/30 dark:text-red-200">
        {error}
      </div>
    );
  }

  if (!selectedEntry.betId || !details) {
    return (
      <div className="flex h-full min-h-[min(78vh,44rem)] items-center justify-center p-4 text-sm text-zinc-500 dark:text-zinc-400">
        This ledger entry is not linked to a bet.
      </div>
    );
  }

  return (
      <article className="max-h-[min(78vh,44rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800/80">
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(details.statusName)}`}
            >
              {details.statusName}
            </span>
            <time
              dateTime={details.betCreatedAt}
              className="tabular-nums text-sm text-zinc-600 dark:text-zinc-400"
              title="Bet placement time"
            >
              Placed: {formatMatchDate(details.betCreatedAt)}
            </time>
          </div>
          {details.agentSessionId != null ? (
            <Link
              href={`/agent?widget=sessions&sessionId=${details.agentSessionId}`}
              className="inline-flex shrink-0 items-center gap-1 rounded-md border border-zinc-300 bg-zinc-700 px-3 py-1.5 text-xs font-medium text-white shadow-sm transition-colors hover:bg-zinc-800 dark:border-zinc-600 dark:bg-zinc-700 dark:hover:bg-zinc-600"
            >
              Session #{details.agentSessionId}
              <ChevronRight className="h-3.5 w-3.5 text-white/90" aria-hidden />
            </Link>
          ) : null}
        </div>

        <div className="grid grid-cols-3 gap-3 border-b border-zinc-100 px-4 py-3 text-sm dark:border-zinc-800/80">
          <div>
            <span className="text-zinc-500 dark:text-zinc-400">Stake</span>
            <p className="font-semibold tabular-nums text-foreground">{formatCurrency(details.stakeAmount)}</p>
          </div>
          <div>
            <span className="text-zinc-500 dark:text-zinc-400">Combined odds</span>
            <p className="font-semibold tabular-nums text-foreground">{details.totalOdds.toFixed(2)}</p>
          </div>
          <div>
            <span className="text-zinc-500 dark:text-zinc-400">Potential payout</span>
            <p className="font-semibold tabular-nums text-foreground">{formatCurrency(details.potentialPayout)}</p>
          </div>
        </div>

        <ul className="px-4 py-3">
          {details.selections.map((selection, idx) => {
            const homeLogoSlug = clubLogoSlugSegment(
              selection.homeClubSlug,
              selection.homeClubName
            );
            const awayLogoSlug = clubLogoSlugSegment(
              selection.awayClubSlug,
              selection.awayClubName
            );
            return (
              <li
                key={`${selection.matchId}-${idx}`}
                className="border-t border-zinc-100 py-2 first:border-t-0 first:pt-0 last:pb-0 dark:border-zinc-800/80"
              >
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
                      className={`inline-flex shrink-0 items-center rounded-md px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${getStatusBadgeClass(selection.statusName)}`}
                    >
                      {selection.statusName}
                    </span>
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      </article>
  );
}
