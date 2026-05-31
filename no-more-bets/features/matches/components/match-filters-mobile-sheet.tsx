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
  MatchFiltersPanel,
  type MatchFiltersPanelProps,
} from "./match-filters-panel";

type MatchFiltersMobileSheetProps = Omit<MatchFiltersPanelProps, "onFilterApplied">;

export function MatchFiltersMobileSheet(props: MatchFiltersMobileSheetProps) {
  const [open, setOpen] = useState(false);

  const activeFilterCount = useMemo(() => {
    let count = 0;
    if (props.selectedLeagueIds.length > 0) {
      count += props.selectedLeagueIds.length;
    }
    if (props.selectedStatusId !== ALL_STATUSES_ID) {
      count += 1;
    }
    return count;
  }, [props.selectedLeagueIds, props.selectedStatusId]);

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
              {...props}
              onFilterApplied={() => setOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>
    </>
  );
}
