"use client";

import { formatCurrency } from "@/utils/format-currency";
import type { AgentDashboardBankrollWidget } from "@/features/bets/interfaces";
import { pickAgentDashboardCopy } from "../_lib/agent-copy";
import { useMemo } from "react";

export function AgentDashboardGreeting({
  bankroll,
}: {
  bankroll: AgentDashboardBankrollWidget | null;
}) {
  const flavor = useMemo(() => pickAgentDashboardCopy().line, []);

  return (
    <header className="flex flex-col gap-1">
      <h1 className="text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">
        Public bankroll and betting log
      </h1>
      <p className="max-w-3xl text-sm leading-6 text-zinc-600 dark:text-zinc-300 sm:text-base">
        Live bankroll
        {bankroll ? ` ${formatCurrency(bankroll.totalValue)}` : ""}
        , pending slips, sessions, and memories from the No More Bets agent. Not betting advice.
      </p>
      <p className="max-w-3xl text-sm leading-6 text-zinc-500 dark:text-zinc-400">{flavor}</p>
    </header>
  );
}
