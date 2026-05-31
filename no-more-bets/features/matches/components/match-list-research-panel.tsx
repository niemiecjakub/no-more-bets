"use client";

import { useEffect, useState } from "react";
import { ResearchBetSlipSummary } from "@/features/bets/components/research-bet-slip-summary";
import type { BetSlipSummaryDto } from "@/features/bets/interfaces";
import {
  fetchMatchAgentResearch,
  fetchMatchResearchBetSlip,
} from "../services/match-insights-api";

interface MatchListResearchPanelProps {
  matchId: number;
}

export function MatchListResearchPanel({ matchId }: MatchListResearchPanelProps) {
  const [research, setResearch] = useState<string | null | undefined>(undefined);
  const [researchError, setResearchError] = useState<string | undefined>();
  const [slip, setSlip] = useState<BetSlipSummaryDto | null | undefined>(undefined);
  const [slipError, setSlipError] = useState<string | undefined>();

  useEffect(() => {
    let cancelled = false;

    async function load() {
      const [researchResult, slipResult] = await Promise.allSettled([
        fetchMatchAgentResearch(matchId),
        fetchMatchResearchBetSlip(matchId),
      ]);

      if (cancelled) return;

      if (researchResult.status === "fulfilled") {
        setResearch(researchResult.value);
      } else {
        setResearchError("Failed to load agent research.");
      }

      if (slipResult.status === "fulfilled") {
        setSlip(slipResult.value);
      } else {
        setSlipError("Failed to load research bet slip.");
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [matchId]);

  const researchLoading = research === undefined && researchError == null;
  const slipLoading = slip === undefined && slipError == null;

  return (
    <div className="border-t border-zinc-200 bg-zinc-50 px-4 py-4 dark:border-zinc-800 dark:bg-zinc-900/50">
      <section className="flex flex-col gap-2">
        <h4 className="text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Research
        </h4>
        {researchError ? (
          <p className="text-sm text-red-800 dark:text-red-200">{researchError}</p>
        ) : researchLoading ? (
          <p className="text-sm text-zinc-500 dark:text-zinc-400">Loading agent research…</p>
        ) : research == null || research === "" ? (
          <p className="text-sm text-zinc-500 dark:text-zinc-400">No agent research available.</p>
        ) : (
          <p className="whitespace-pre-wrap text-sm leading-6 text-zinc-700 dark:text-zinc-300">
            {research}
          </p>
        )}
      </section>

      <section className="mt-4 flex flex-col gap-2 border-t border-zinc-200 pt-4 dark:border-zinc-800">
        <h4 className="text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
          Research bet
        </h4>
        <ResearchBetSlipSummary
          slip={slip ?? null}
          isLoading={slipLoading}
          error={slipError}
          variant="matchPage"
        />
      </section>
    </div>
  );
}
