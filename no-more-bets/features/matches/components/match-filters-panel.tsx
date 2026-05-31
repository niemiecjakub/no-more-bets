"use client";

import { LeagueList } from "@/features/leagues/components/league-list";
import type { LeagueListItem } from "@/features/leagues/interfaces";
import { MATCH_STATUS } from "../interfaces";

export const ALL_STATUSES_ID = -1;

export const statusFilters = [
  { id: ALL_STATUSES_ID, label: "All" },
  { id: MATCH_STATUS.Upcoming, label: "Upcoming" },
  { id: MATCH_STATUS.Finished, label: "Finished" },
] as const;

function LeaguesFallback() {
  return (
    <div className="grid animate-pulse grid-cols-1 gap-3">
      {[1, 2, 3, 4, 5].map((i) => (
        <div
          key={i}
          className="relative overflow-hidden rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950"
        >
          <div className="absolute left-0 top-0 h-1 w-full bg-zinc-200 dark:bg-zinc-800" />
          <div className="mt-1 flex items-center gap-3">
            <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

export interface MatchFiltersPanelProps {
  leagues: LeagueListItem[];
  isLeaguesLoading: boolean;
  leaguesError: string | null;
  selectedLeagueIds: number[];
  selectedStatusId: number;
  onToggleLeague: (id: number) => void;
  onSelectStatus: (id: number) => void;
  onFilterApplied?: () => void;
}

export function MatchFiltersPanel({
  leagues,
  isLeaguesLoading,
  leaguesError,
  selectedLeagueIds,
  selectedStatusId,
  onToggleLeague,
  onSelectStatus,
  onFilterApplied,
}: MatchFiltersPanelProps) {
  function handleToggleLeague(leagueId: number) {
    onToggleLeague(leagueId);
    onFilterApplied?.();
  }

  function handleSelectStatus(statusId: number) {
    onSelectStatus(statusId);
    onFilterApplied?.();
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
        <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Match status
        </p>
        <div className="grid grid-cols-3 gap-1.5">
          {statusFilters.map((statusFilter) => {
            const selected = statusFilter.id === selectedStatusId;
            return (
              <button
                key={statusFilter.id}
                type="button"
                onClick={() => handleSelectStatus(statusFilter.id)}
                aria-pressed={selected}
                className={`min-w-0 rounded-md px-1.5 py-2 text-center text-xs font-medium transition-colors ${
                  selected
                    ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
                    : "bg-zinc-100 text-zinc-700 hover:bg-zinc-200 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
                }`}
              >
                {statusFilter.label}
              </button>
            );
          })}
        </div>
      </div>
      <h3 className="px-1 text-sm font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
        Leagues
      </h3>
      {isLeaguesLoading && leagues.length === 0 ? (
        <LeaguesFallback />
      ) : leaguesError ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
          {leaguesError}
        </p>
      ) : (
        <LeagueList
          leagues={leagues}
          selectedLeagueIds={selectedLeagueIds}
          onToggleLeague={handleToggleLeague}
          className="grid grid-cols-1 gap-0"
        />
      )}
    </div>
  );
}
