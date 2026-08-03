"use client";

import { useMemo, useState } from "react";
import { SlidersHorizontal } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  ALL_STATUSES_ID,
  isExplicitSeasonOverride,
  isExplicitSortOverride,
  MatchFiltersPanel,
  type MatchFiltersPanelProps,
} from "./match-filters-panel";

type MatchFiltersMobileSheetProps = Omit<MatchFiltersPanelProps, "onFilterApplied"> & {
  sortParam: string | null;
  latestSeasonYear: string | null;
};

export function MatchFiltersMobileSheet(props: MatchFiltersMobileSheetProps) {
  const { sortParam, latestSeasonYear, ...panelProps } = props;
  const [open, setOpen] = useState(false);

  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (panelProps.selectedLeagueIds.length > 0) {
      count += panelProps.selectedLeagueIds.length;
    }
    if (panelProps.selectedStatusId !== ALL_STATUSES_ID) {
      count += 1;
    }
    if (isExplicitSortOverride(sortParam, panelProps.selectedStatusId)) {
      count += 1;
    }
    if (isExplicitSeasonOverride(panelProps.selectedSeasonYears, latestSeasonYear)) {
      count += 1;
    }
    if (panelProps.searchQuery.trim().length > 0) {
      count += 1;
    }
    return count;
  }, [
    panelProps.selectedLeagueIds,
    panelProps.selectedStatusId,
    panelProps.searchQuery,
    panelProps.selectedSeasonYears,
    sortParam,
    latestSeasonYear,
  ]);

  return (
    <>
      <button
        type="button"
        className="flex w-full items-center justify-between gap-2 rounded-lg border border-zinc-200 bg-white px-3.5 py-2.5 text-sm font-medium text-foreground transition-colors hover:border-zinc-300 hover:bg-zinc-50 active:bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:hover:border-zinc-700 dark:hover:bg-zinc-900"
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={() => setOpen(true)}
      >
        <span className="flex items-center gap-2">
          <SlidersHorizontal className="size-4 text-zinc-500 dark:text-zinc-400" aria-hidden />
          Filters
        </span>
        {activeFilterCount > 0 ? (
          <span className="rounded-full bg-zinc-900 px-2 py-0.5 text-xs font-medium text-white dark:bg-zinc-100 dark:text-zinc-900">
            {activeFilterCount}
          </span>
        ) : null}
      </button>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="bottom" className="max-h-[85dvh] overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Filters</SheetTitle>
          </SheetHeader>
          <div className="px-4 pb-6">
            <MatchFiltersPanel
              {...panelProps}
              showTitle={false}
              onFilterApplied={() => setOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>
    </>
  );
}
