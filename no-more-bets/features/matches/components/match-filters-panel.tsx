"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { LeagueList } from "@/features/leagues/components/league-list";
import type { LeagueListItem } from "@/features/leagues/interfaces";
import { cn } from "@/lib/utils";
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

export function isExplicitSeasonOverride(
  selectedSeasonYears: string[],
  latestSeasonYear: string | null,
): boolean {
  if (!latestSeasonYear) return selectedSeasonYears.length > 0;
  if (selectedSeasonYears.length !== 1) return true;
  return selectedSeasonYears[0] !== latestSeasonYear;
}

function areSeasonYearsEqual(left: string[], right: string[]): boolean {
  if (left.length !== right.length) return false;
  const leftSorted = [...left].sort();
  const rightSorted = [...right].sort();
  return leftSorted.every((year, index) => year === rightSorted[index]);
}

function buildSeasonTriggerLabel(selectedSeasonYears: string[]): string {
  if (selectedSeasonYears.length === 0) return "All seasons";
  if (selectedSeasonYears.length === 1) return selectedSeasonYears[0] ?? "1 season selected";
  if (selectedSeasonYears.length === 2) return selectedSeasonYears.join(", ");
  return `${selectedSeasonYears.length} seasons selected`;
}

function SeasonMultiSelect({
  seasonYears,
  selectedSeasonYears,
  onSelectedSeasonYearsChange,
}: {
  seasonYears: string[];
  selectedSeasonYears: string[];
  onSelectedSeasonYearsChange: (years: string[]) => void;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [draftYears, setDraftYears] = useState<string[]>(selectedSeasonYears);
  const draftYearsRef = useRef(draftYears);
  const rootRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();
  const hasActiveFilters = selectedSeasonYears.length > 0;
  const triggerLabel = useMemo(
    () => buildSeasonTriggerLabel(selectedSeasonYears),
    [selectedSeasonYears],
  );

  draftYearsRef.current = draftYears;

  const commitDraft = useCallback(
    (nextDraft: string[]) => {
      if (!areSeasonYearsEqual(nextDraft, selectedSeasonYears)) {
        onSelectedSeasonYearsChange(nextDraft);
      }
    },
    [onSelectedSeasonYearsChange, selectedSeasonYears],
  );

  const closeDropdown = useCallback(
    (nextDraft: string[]) => {
      setIsOpen(false);
      commitDraft(nextDraft);
    },
    [commitDraft],
  );

  function openDropdown() {
    setDraftYears(selectedSeasonYears);
    setIsOpen(true);
  }

  function toggleDropdown() {
    if (isOpen) closeDropdown(draftYearsRef.current);
    else openDropdown();
  }

  function toggleDraftYear(year: string) {
    setDraftYears((current) =>
      current.includes(year)
        ? current.filter((item) => item !== year)
        : [...current, year],
    );
  }

  useEffect(() => {
    if (!isOpen) return;

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        closeDropdown(draftYearsRef.current);
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") closeDropdown(draftYearsRef.current);
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [closeDropdown, isOpen]);

  return (
    <div ref={rootRef} className="relative">
      <div className="mb-2 flex items-center justify-between gap-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Season
        </p>
        {hasActiveFilters ? (
          <button
            type="button"
            onClick={() => {
              if (isOpen) closeDropdown([]);
              else onSelectedSeasonYearsChange([]);
            }}
            className="text-xs font-medium text-zinc-600 underline-offset-2 hover:underline dark:text-zinc-300"
          >
            Clear
          </button>
        ) : null}
      </div>

      <Button
        type="button"
        variant="outline"
        size="sm"
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={listboxId}
        onClick={toggleDropdown}
        disabled={seasonYears.length === 0}
        className="h-9 w-full justify-between px-2.5 font-normal text-zinc-700 dark:text-zinc-200"
      >
        <span className="truncate">{triggerLabel}</span>
        <ChevronDown
          className={cn("shrink-0 text-zinc-500 transition-transform", isOpen && "rotate-180")}
          aria-hidden
        />
      </Button>

      {isOpen ? (
        <div
          id={listboxId}
          role="listbox"
          aria-multiselectable="true"
          aria-label="Seasons"
          className="absolute top-[calc(100%+0.25rem)] right-0 left-0 z-20 overflow-hidden rounded-md border border-zinc-200 bg-white shadow-lg dark:border-zinc-700 dark:bg-zinc-950"
        >
          <ul className="max-h-56 overflow-y-auto py-1">
            {seasonYears.map((year) => {
              const selected = draftYears.includes(year);
              return (
                <li key={year} role="presentation">
                  <button
                    type="button"
                    role="option"
                    aria-selected={selected}
                    onClick={() => toggleDraftYear(year)}
                    className={cn(
                      "flex w-full items-center gap-2 px-2.5 py-2 text-left text-sm transition-colors",
                      selected
                        ? "bg-zinc-100 text-zinc-900 dark:bg-zinc-900 dark:text-zinc-100"
                        : "text-zinc-700 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-900/80",
                    )}
                  >
                    <span
                      className={cn(
                        "flex size-4 shrink-0 items-center justify-center rounded border",
                        selected
                          ? "border-zinc-900 bg-zinc-900 text-white dark:border-zinc-100 dark:bg-zinc-100 dark:text-zinc-900"
                          : "border-zinc-300 bg-white dark:border-zinc-600 dark:bg-zinc-950",
                      )}
                      aria-hidden
                    >
                      {selected ? <Check className="size-3" /> : null}
                    </span>
                    <span className="min-w-0 flex-1 truncate">{year}</span>
                  </button>
                </li>
              );
            })}
          </ul>
          {draftYears.length > 0 ? (
            <div className="border-t border-zinc-100 px-2 py-1.5 dark:border-zinc-800">
              <button
                type="button"
                onClick={() => closeDropdown([])}
                className="w-full rounded-md px-2 py-1.5 text-left text-xs font-medium text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-900/80"
              >
                Clear selection
              </button>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
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
      placeholder="Search matches…"
      aria-label="Search matches"
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
  seasonYears: string[];
  isSeasonYearsLoading: boolean;
  seasonYearsError: string | null;
  selectedSeasonYears: string[];
  onToggleLeague: (id: number) => void;
  onSelectStatus: (id: number) => void;
  onSelectSort: (sortOrder: MatchDateSortOrder) => void;
  onSearchQueryChange: (value: string) => void;
  onSelectedSeasonYearsChange: (years: string[]) => void;
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
  seasonYears,
  isSeasonYearsLoading,
  seasonYearsError,
  selectedSeasonYears,
  onToggleLeague,
  onSelectStatus,
  onSelectSort,
  onSearchQueryChange,
  onSelectedSeasonYearsChange,
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

  function handleSelectedSeasonYearsChange(years: string[]) {
    onSelectedSeasonYearsChange(years);
    onFilterApplied?.();
  }

  return (
    <div className="flex flex-col gap-3">
      {showTitle ? (
        <h2 className="px-1 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Filters
        </h2>
      ) : null}
      <div className="relative z-10 overflow-visible rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
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
            {isSeasonYearsLoading && seasonYears.length === 0 ? (
              <>
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
                  Season
                </p>
                <div className="h-9 animate-pulse rounded-md bg-zinc-100 dark:bg-zinc-900" />
              </>
            ) : seasonYearsError ? (
              <>
                <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
                  Season
                </p>
                <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                  {seasonYearsError}
                </p>
              </>
            ) : (
              <SeasonMultiSelect
                seasonYears={seasonYears}
                selectedSeasonYears={selectedSeasonYears}
                onSelectedSeasonYearsChange={handleSelectedSeasonYearsChange}
              />
            )}
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
