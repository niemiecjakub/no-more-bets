"use client";

import { useEffect, useState } from "react";
import type {
  BankrollEntryBetDetailsDto,
  BankrollEntryListItemDto,
} from "@/features/bets/interfaces";
import {
  fetchBankrollEntries,
  fetchBankrollEntryBetDetails,
} from "@/features/bets/services/bankroll-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentBankrollEntriesList } from "./agent-bankroll-entries-list";
import { AgentBankrollRelatedBet } from "./agent-bankroll-related-bet";

export function AgentBankrollDetailsPanel() {
  const [entries, setEntries] = useState<BankrollEntryListItemDto[]>([]);
  const [selectedEntry, setSelectedEntry] = useState<BankrollEntryListItemDto | null>(null);
  const [selectedBetDetails, setSelectedBetDetails] = useState<BankrollEntryBetDetailsDto | null>(null);

  const [isEntriesLoading, setIsEntriesLoading] = useState(true);
  const [isBetDetailsLoading, setIsBetDetailsLoading] = useState(false);

  const [entriesError, setEntriesError] = useState<string | null>(null);
  const [betDetailsError, setBetDetailsError] = useState<string | null>(null);

  function loadBetDetailsForEntry(entry: BankrollEntryListItemDto | null) {
    setSelectedEntry(entry);
    setSelectedBetDetails(null);
    setBetDetailsError(null);

    if (!entry || !entry.betId) {
      setIsBetDetailsLoading(false);
      return;
    }

    setIsBetDetailsLoading(true);
    fetchBankrollEntryBetDetails(entry.id)
      .then((data) => {
        setSelectedBetDetails(data);
      })
      .catch((caughtError) => {
        setSelectedBetDetails(null);
        setBetDetailsError(handleServiceError(caughtError, "Failed to load related bet."));
      })
      .finally(() => {
        setIsBetDetailsLoading(false);
      });
  }

  useEffect(() => {
    let cancelled = false;

    fetchBankrollEntries()
      .then((data) => {
        if (cancelled) return;
        setEntries(data);
        const firstWithBet = data.find((entry) => entry.betId !== null) ?? data[0] ?? null;
        loadBetDetailsForEntry(firstWithBet);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setEntriesError(handleServiceError(caughtError, "Failed to load bankroll entries."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsEntriesLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section className="flex flex-col gap-4">
      <div className="grid min-h-[min(78vh,44rem)] grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
        <section className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
          {entriesError ? (
            <p className="m-4 rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
              {entriesError}
            </p>
          ) : (
            <AgentBankrollEntriesList
              entries={entries}
              selectedEntryId={selectedEntry?.id ?? null}
              onSelectEntry={loadBetDetailsForEntry}
              isLoading={isEntriesLoading}
            />
          )}
        </section>

        <section className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
          <AgentBankrollRelatedBet
            selectedEntry={selectedEntry}
            details={selectedBetDetails}
            isLoading={isBetDetailsLoading}
            error={betDetailsError}
          />
        </section>
      </div>
    </section>
  );
}
