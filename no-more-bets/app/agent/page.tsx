"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { AgentBetsTab } from "./_components/agent-bets-tab";
import { AgentMemoriesTab } from "./_components/agent-memories-tab";
import { AgentSessionsTab } from "./_components/agent-sessions-tab";

type AgentTabId = "bets" | "sessions" | "memories";

interface AgentTab {
  id: AgentTabId;
  label: string;
}

const AGENT_TABS: AgentTab[] = [
  { id: "bets", label: "Bets" },
  { id: "sessions", label: "Sessions" },
  { id: "memories", label: "Memories" },
];

function isAgentTab(value: string | null): value is AgentTabId {
  return value === "bets" || value === "sessions" || value === "memories";
}

export default function AgentPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const tabFromQuery = searchParams.get("tab");
  const activeTab: AgentTabId = isAgentTab(tabFromQuery) ? tabFromQuery : "bets";

  function handleTabChange(nextTab: AgentTabId) {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", nextTab);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-4 text-2xl font-semibold tracking-tight text-foreground">Agent</h1>

        <nav className="mb-6 flex flex-wrap gap-2" aria-label="Agent sections">
          {AGENT_TABS.map((tab) => {
            const isActive = tab.id === activeTab;
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => handleTabChange(tab.id)}
                className={
                  "rounded-md border px-3 py-2 text-sm font-medium transition-colors " +
                  (isActive
                    ? "border-zinc-300 bg-zinc-100 text-zinc-900 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-100 dark:ring-zinc-500/30"
                    : "border-zinc-200 bg-white text-zinc-700 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900/80")
                }
                aria-current={isActive ? "page" : undefined}
              >
                {tab.label}
              </button>
            );
          })}
        </nav>

        {activeTab === "bets" ? <AgentBetsTab /> : null}
        {activeTab === "sessions" ? <AgentSessionsTab /> : null}
        {activeTab === "memories" ? <AgentMemoriesTab /> : null}
      </main>
    </div>
  );
}
