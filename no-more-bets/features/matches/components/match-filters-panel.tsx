"use client";

import { useEffect, useState } from "react";
import { Input } from "@/components/ui/input";
import { LeagueList } from "@/features/leagues/components/league-list";
import type { LeagueListItem } from "@/features/leagues/interfaces";
import { MATCH_STATUS } from "../interfaces";
import {
  getDefaultSortForStatus,
  MATCH_DATE_SORT,
  type MatchDateSortOrder,
} from "../services/matches-api";

export const ALL_STATUSES_ID = -1;

export const statusFilters = [
  { id: ALL_STATUSES_ID, label: "All" },
  { id: MATCH_STATUS.Upcoming, label: "Upcoming" },
  { id: MATCH_STATUS.Finished, label: "Finished" },
] as const;

export const sortFilters = [
  { id: MATCH_DATE_SORT.Ascending, label: "Ascending" },
  { id: MATCH_DATE_SORT.Descending, label: "Descending" },
] as const;

export function parseSortOrderParam(
  value: string | null,
  statusId: number,
): MatchDateSortOrder {
  if (value === MATCH_DATE_SORT.Ascending || value === MATCH_DATE_SORT.Descending) {
    return value;
  }
  return getDefaultSortForStatus(statusId);
}

export function isExplicitSortOverride(
  value: string | null,
  statusId: number,
): boolean {
  if (value !== MATCH_DATE_SORT.Ascending && value !== MATCH_DATE_SORT.Descending) {
    return false;
  }
  return value !== getDefaultSortForStatus(statusId);
}

function MatchSearchField({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) {
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (draft !== value) {
        onChange(draft);
      }
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [draft, value, onChange]);

  return (
    <Input
      type="search"
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      placeholder="Search club..."
      aria-label="Search by club"
      className="h-8 bg-white dark:bg-zinc-950"
    />
  );
}

function LeaguesFallback() {
  return (
    <div className="animate-pulse divide-y divide-zinc-200 dark:divide-zinc-800">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="flex items-center gap-3 px-3 py-3">
          <div className="h-6 w-6 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 max-w-xs flex-1 rounded bg-zinc-200 dark:bg-zinc-800" />
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
  selectedSortOrder: MatchDateSortOrder;
  searchQuery: string;
  onToggleLeague: (id: number) => void;
  onSelectStatus: (id: number) => void;
  onSelectSort: (sortOrder: MatchDateSortOrder) => void;
  onSearchQueryChange: (value: string) => void;
  onFilterApplied?: () => void;
  showTitle?: boolean;
}

export function MatchFiltersPanel({
  leagues,
  isLeaguesLoading,
  leaguesError,
  selectedLeagueIds,
  selectedStatusId,
  selectedSortOrder,
  searchQuery,
  onToggleLeague,
  onSelectStatus,
  onSelectSort,
  onSearchQueryChange,
  onFilterApplied,
  showTitle = true,
}: MatchFiltersPanelProps) {
  function handleToggleLeague(leagueId: number) {
    onToggleLeague(leagueId);
    onFilterApplied?.();
  }

  function handleSelectStatus(statusId: number) {
    onSelectStatus(statusId);
    onFilterApplied?.();
  }

  function handleSelectSort(sortOrder: MatchDateSortOrder) {
    onSelectSort(sortOrder);
    onFilterApplied?.();
  }

  return (
    <div className="flex flex-col gap-3">
      {showTitle ? (
        <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Filters
        </h2>
      ) : null}
      <div className="rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
        <div className="flex flex-col gap-3">
          <div>
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Search
            </p>
            <MatchSearchField value={searchQuery} onChange={onSearchQueryChange} />
          </div>
          <div className="border-t border-zinc-100 pt-3 dark:border-zinc-800">
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
          <div className="border-t border-zinc-100 pt-3 dark:border-zinc-800">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Sort by date
            </p>
            <div className="grid grid-cols-2 gap-1.5">
              {sortFilters.map((sortFilter) => {
                const selected = sortFilter.id === selectedSortOrder;
                return (
                  <button
                    key={sortFilter.id}
                    type="button"
                    onClick={() => handleSelectSort(sortFilter.id)}
                    aria-pressed={selected}
                    className={`min-w-0 rounded-md px-1.5 py-2 text-center text-xs font-medium transition-colors ${
                      selected
                        ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900"
                        : "bg-zinc-100 text-zinc-700 hover:bg-zinc-200 dark:bg-zinc-900 dark:text-zinc-300 dark:hover:bg-zinc-800"
                    }`}
                  >
                    {sortFilter.label}
                  </button>
                );
              })}
            </div>
          </div>
          <div className="border-t border-zinc-100 pt-3 dark:border-zinc-800">
            <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
              Leagues
            </p>
            {isLeaguesLoading && leagues.length === 0 ? (
              <div className="-mx-3">
                <LeaguesFallback />
              </div>
            ) : leaguesError ? (
              <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                {leaguesError}
              </p>
            ) : (
              <div className="-mx-3">
                <LeagueList
                  leagues={leagues}
                  selectedLeagueIds={selectedLeagueIds}
                  onToggleLeague={handleToggleLeague}
                />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
