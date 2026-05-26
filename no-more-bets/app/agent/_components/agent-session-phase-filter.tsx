"use client";

import { useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { AGENT_SESSION_PHASES } from "@/features/sessions/agent-session-phases";
import { cn } from "@/lib/utils";

interface AgentSessionPhaseFilterProps {
  selectedPhaseIds: number[];
  onSelectedPhaseIdsChange: (phaseIds: number[]) => void;
}

function buildTriggerLabel(selectedPhaseIds: number[]): string {
  if (selectedPhaseIds.length === 0) return "All session types";

  const selectedLabels = AGENT_SESSION_PHASES.filter((phase) => selectedPhaseIds.includes(phase.id)).map(
    (phase) => phase.label,
  );

  if (selectedLabels.length === 1) return selectedLabels[0] ?? "1 type selected";
  if (selectedLabels.length === 2) return selectedLabels.join(", ");
  return `${selectedLabels.length} types selected`;
}

function arePhaseIdsEqual(left: number[], right: number[]): boolean {
  if (left.length !== right.length) return false;
  const leftSorted = [...left].sort((a, b) => a - b);
  const rightSorted = [...right].sort((a, b) => a - b);
  return leftSorted.every((id, index) => id === rightSorted[index]);
}

export function AgentSessionPhaseFilter({
  selectedPhaseIds,
  onSelectedPhaseIdsChange,
}: AgentSessionPhaseFilterProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [draftPhaseIds, setDraftPhaseIds] = useState<number[]>(selectedPhaseIds);
  const draftPhaseIdsRef = useRef(draftPhaseIds);
  const rootRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();
  const hasActiveFilters = selectedPhaseIds.length > 0;
  const triggerLabel = useMemo(() => buildTriggerLabel(selectedPhaseIds), [selectedPhaseIds]);

  draftPhaseIdsRef.current = draftPhaseIds;

  const commitDraft = useCallback(
    (nextDraft: number[]) => {
      if (!arePhaseIdsEqual(nextDraft, selectedPhaseIds)) {
        onSelectedPhaseIdsChange(nextDraft);
      }
    },
    [onSelectedPhaseIdsChange, selectedPhaseIds],
  );

  const closeDropdown = useCallback(
    (nextDraft: number[]) => {
      setIsOpen(false);
      commitDraft(nextDraft);
    },
    [commitDraft],
  );

  function openDropdown() {
    setDraftPhaseIds(selectedPhaseIds);
    setIsOpen(true);
  }

  function toggleDropdown() {
    if (isOpen) closeDropdown(draftPhaseIdsRef.current);
    else openDropdown();
  }

  function toggleDraftPhase(phaseId: number) {
    setDraftPhaseIds((current) =>
      current.includes(phaseId) ? current.filter((id) => id !== phaseId) : [...current, phaseId],
    );
  }

  useEffect(() => {
    if (!isOpen) return;

    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        closeDropdown(draftPhaseIdsRef.current);
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") closeDropdown(draftPhaseIdsRef.current);
    }

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [closeDropdown, isOpen]);

  return (
    <div ref={rootRef} className="relative border-b border-zinc-100 px-3 py-3 dark:border-zinc-800">
      <div className="mb-2 flex items-center justify-between gap-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Session type
        </p>
        {hasActiveFilters ? (
          <button
            type="button"
            onClick={() => {
              if (isOpen) closeDropdown([]);
              else onSelectedPhaseIdsChange([]);
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
          aria-label="Session types"
          className="absolute top-[calc(100%-0.25rem)] right-3 left-3 z-20 overflow-hidden rounded-md border border-zinc-200 bg-white shadow-lg dark:border-zinc-700 dark:bg-zinc-950"
        >
          <ul className="max-h-56 overflow-y-auto py-1">
            {AGENT_SESSION_PHASES.map((phase) => {
              const selected = draftPhaseIds.includes(phase.id);
              const PhaseIcon = phase.icon;
              return (
                <li key={phase.id} role="presentation">
                  <button
                    type="button"
                    role="option"
                    aria-selected={selected}
                    onClick={() => toggleDraftPhase(phase.id)}
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
                    <PhaseIcon className="size-4 shrink-0 text-zinc-500 dark:text-zinc-400" aria-hidden />
                    <span className="min-w-0 flex-1 truncate">{phase.label}</span>
                  </button>
                </li>
              );
            })}
          </ul>
          {draftPhaseIds.length > 0 ? (
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
