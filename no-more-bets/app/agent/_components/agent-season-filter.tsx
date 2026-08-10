"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

interface AgentSeasonFilterProps {
  seasonYears: string[];
  selectedSeasonYears: string[];
  onSelectedSeasonYearsChange: (years: string[]) => void;
  isLoading?: boolean;
  error?: string | null;
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

export function AgentSeasonFilter({
  seasonYears,
  selectedSeasonYears,
  onSelectedSeasonYearsChange,
  isLoading = false,
  error = null,
}: AgentSeasonFilterProps) {
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
      current.includes(year) ? current.filter((item) => item !== year) : [...current, year],
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

  if (error) {
    return (
      <p className="text-sm text-red-600 dark:text-red-400" role="alert">
        {error}
      </p>
    );
  }

  return (
    <div ref={rootRef} className="relative w-full lg:max-w-xs">
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
        disabled={isLoading || seasonYears.length === 0}
        className="h-9 w-full justify-between px-2.5 font-normal text-zinc-700 dark:text-zinc-200"
      >
        <span className="truncate">
          {isLoading && seasonYears.length === 0 ? "Loading seasons…" : triggerLabel}
        </span>
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
