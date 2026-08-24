"use client";

import { useMemo } from "react";
import { pickAgentDashboardCopy } from "../_lib/agent-copy";

export function AgentDashboardGreeting() {
  const { greeting, line } = useMemo(() => pickAgentDashboardCopy(), []);

  return (
    <header className="flex flex-col gap-0.5">
      <p className="text-lg font-semibold tracking-tight text-zinc-500 dark:text-zinc-400 sm:text-xl">
        {greeting}
      </p>
      <p className="max-w-3xl text-sm leading-6 text-zinc-500 dark:text-zinc-400 sm:text-base sm:leading-7">
        {line}
      </p>
    </header>
  );
}
