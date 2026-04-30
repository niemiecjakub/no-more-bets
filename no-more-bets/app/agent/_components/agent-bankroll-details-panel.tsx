"use client";

import { useEffect, useState } from "react";
import type {
  BankrollEntryBetDetailsDto,
  BankrollEntryListItemDto,
  BankrollFlowPointDto,
} from "@/features/bets/interfaces";
import {
  fetchBankrollEntries,
  fetchBankrollEntryBetDetails,
  fetchBankrollFlowPoints,
} from "@/features/bets/services/bankroll-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentBankrollEntriesList } from "./agent-bankroll-entries-list";
import { AgentBankrollFlowChart } from "./agent-bankroll-flow-chart";
import { AgentBankrollRelatedBet } from "./agent-bankroll-related-bet";

export function AgentBankrollDetailsPanel() {
  const [flowPoints, setFlowPoints] = useState<BankrollFlowPointDto[]>([]);
  const [entries, setEntries] = useState<BankrollEntryListItemDto[]>([]);
  const [selectedEntry, setSelectedEntry] = useState<BankrollEntryListItemDto | null>(null);
  const [selectedBetDetails, setSelectedBetDetails] = useState<BankrollEntryBetDetailsDto | null>(null);

  const [isFlowLoading, setIsFlowLoading] = useState(true);
  const [isEntriesLoading, setIsEntriesLoading] = useState(true);
  const [isBetDetailsLoading, setIsBetDetailsLoading] = useState(false);

  const [flowError, setFlowError] = useState<string | null>(null);
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

    fetchBankrollFlowPoints()
      .then((data) => {
        if (!cancelled) setFlowPoints(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setFlowError(handleServiceError(caughtError, "Failed to load bankroll flow."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsFlowLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

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
      <article className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="grid min-h-[420px] lg:grid-cols-[0.55fr_1.45fr]">
          <section className="lg:border-r lg:border-zinc-200 dark:lg:border-zinc-800">
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

          <section>
            <AgentBankrollRelatedBet
              selectedEntry={selectedEntry}
              details={selectedBetDetails}
              isLoading={isBetDetailsLoading}
              error={betDetailsError}
            />
          </section>
        </div>
      </article>

      <article className="rounded-lg border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950">
        <h4 className="mb-3 text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Money flow
        </h4>
        {flowError ? (
          <p className="rounded-md border border-red-200 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {flowError}
          </p>
        ) : (
          <AgentBankrollFlowChart points={flowPoints} isLoading={isFlowLoading} />
        )}
      </article>
    </section>
  );
}
