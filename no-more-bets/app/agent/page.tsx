"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Bot, ChevronRight, Globe, Lightbulb, Search, Ticket, Trash2 } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";
import { BankrollSidebar } from "@/features/bets/components/bankroll-sidebar";
import { AgentSessionTranscript } from "@/features/bets/components/agent-session-transcript";
import type { BankrollDashboard } from "@/features/bets/interfaces";
import { fetchAgentSessionMessages, type AgentSessionMessage } from "@/features/bets/services/agent-session-api";
import { fetchBankrollDashboard } from "@/features/bets/services/bankroll-api";
import type { MemoryListItem } from "@/features/memories/interfaces";
import { fetchMemories } from "@/features/memories/services/memories-api";
import type { AgentSessionListItem } from "@/features/sessions/interfaces";
import { fetchAgentSessions } from "@/features/sessions/services/sessions-api";
import { handleServiceError } from "@/lib/error-handler";
import { useBetSlipStore } from "@/store/bet-slip-store";

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

function formatDate(iso: string) {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: "medium",
      timeStyle: "short",
    });
  } catch {
    return iso;
  }
}

function sessionPhaseIcon(phaseId: number): LucideIcon {
  switch (phaseId) {
    case 1:
      return Search;
    case 2:
      return Ticket;
    case 3:
      return Lightbulb;
    case 4:
      return Globe;
    case 5:
      return Trash2;
    default:
      return Bot;
  }
}

function BetsFallback() {
  return (
    <div className="animate-pulse space-y-4">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
        >
          <div className="flex gap-2 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
            <div className="h-5 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="grid grid-cols-3 gap-3 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
            <div className="h-8 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-8 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-8 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
          <div className="space-y-2 px-4 py-3">
            <div className="h-4 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-3 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      ))}
    </div>
  );
}

function SessionsFallback() {
  return (
    <div className="grid animate-pulse grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex w-full flex-col gap-2 overflow-hidden rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="w-full rounded-md border border-zinc-100 px-3 py-2.5 dark:border-zinc-800">
            <div className="h-4 w-3/4 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        ))}
      </div>
      <div className="min-h-[min(70vh,36rem)] overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
          <div className="h-6 w-2/3 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
        <div className="space-y-2 p-4">
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </div>
  );
}

function MemoriesFallback() {
  return (
    <div className="grid animate-pulse grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex flex-col gap-2 overflow-hidden rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="rounded-md border border-zinc-100 px-3 py-2.5 dark:border-zinc-800">
            <div className="h-4 w-3/4 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="mt-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        ))}
      </div>
      <div className="min-h-[min(70vh,36rem)] overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="flex items-center justify-between gap-3 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
          <div className="h-6 w-2/5 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 w-36 shrink-0 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
        <div className="space-y-2 p-4">
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </div>
  );
}

export default function AgentPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const tabFromQuery = searchParams.get("tab");
  const activeTab: AgentTabId = isAgentTab(tabFromQuery) ? tabFromQuery : "bets";

  const { betSlips, isLoading: isLoadingBets, error: betsError, setBetSlips } = useBetSlipStore();
  const [bankroll, setBankroll] = useState<BankrollDashboard | null>(null);
  const [bankrollLoading, setBankrollLoading] = useState(true);
  const [bankrollError, setBankrollError] = useState<string | null>(null);

  const [sessions, setSessions] = useState<AgentSessionListItem[]>([]);
  const [selectedSessionId, setSelectedSessionId] = useState<number | null>(null);
  const [isLoadingSessions, setIsLoadingSessions] = useState(true);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [transcriptMessages, setTranscriptMessages] = useState<AgentSessionMessage[] | null>(null);
  const [isLoadingTranscript, setIsLoadingTranscript] = useState(false);
  const [transcriptError, setTranscriptError] = useState<string | null>(null);

  const [memories, setMemories] = useState<MemoryListItem[]>([]);
  const [selectedMemoryId, setSelectedMemoryId] = useState<number | null>(null);
  const [isLoadingMemories, setIsLoadingMemories] = useState(true);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);

  const sortedSessions = useMemo(
    () => [...sessions].sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()),
    [sessions]
  );

  const sortedMemories = useMemo(
    () => [...memories].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()),
    [memories]
  );

  useEffect(() => {
    setBetSlips();
  }, [setBetSlips]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setBankrollLoading(true);
      setBankrollError(null);
      try {
        const data = await fetchBankrollDashboard();
        if (!cancelled) {
          setBankroll(data);
        }
      } catch (error) {
        if (!cancelled) {
          setBankrollError(handleServiceError(error, "Failed to load bankroll."));
        }
      } finally {
        if (!cancelled) {
          setBankrollLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoadingSessions(true);
      setSessionsError(null);
      try {
        const data = await fetchAgentSessions();
        if (!cancelled) {
          setSessions(data);
        }
      } catch (error) {
        if (!cancelled) {
          setSessionsError(handleServiceError(error, "Failed to load agent sessions."));
        }
      } finally {
        if (!cancelled) {
          setIsLoadingSessions(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoadingMemories(true);
      setMemoriesError(null);
      try {
        const data = await fetchMemories();
        if (!cancelled) {
          setMemories(data);
        }
      } catch (error) {
        if (!cancelled) {
          setMemoriesError(handleServiceError(error, "Failed to load memories."));
        }
      } finally {
        if (!cancelled) {
          setIsLoadingMemories(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setSelectedSessionId((previous) => {
      if (sortedSessions.length === 0) return null;
      if (previous != null && sortedSessions.some((session) => session.id === previous)) return previous;
      return sortedSessions[0].id;
    });
  }, [sortedSessions]);

  useEffect(() => {
    setSelectedMemoryId((previous) => {
      if (sortedMemories.length === 0) return null;
      if (previous != null && sortedMemories.some((memory) => memory.id === previous)) return previous;
      return sortedMemories[0].id;
    });
  }, [sortedMemories]);

  useEffect(() => {
    if (selectedSessionId == null) {
      setTranscriptMessages(null);
      setTranscriptError(null);
      setIsLoadingTranscript(false);
      return;
    }

    let cancelled = false;
    setTranscriptMessages(null);
    setTranscriptError(null);
    setIsLoadingTranscript(true);

    void fetchAgentSessionMessages(selectedSessionId)
      .then((data) => {
        if (!cancelled) {
          setTranscriptMessages(data);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setTranscriptError(handleServiceError(error, "Failed to load session transcript."));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsLoadingTranscript(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selectedSessionId]);

  const selectedSession = selectedSessionId != null ? sortedSessions.find((session) => session.id === selectedSessionId) : undefined;
  const selectedMemory = selectedMemoryId != null ? sortedMemories.find((memory) => memory.id === selectedMemoryId) : undefined;
  const SelectedPhaseIcon = selectedSession ? sessionPhaseIcon(selectedSession.phaseId) : Bot;

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

        {activeTab === "bets" ? (
          <div className="grid gap-8 lg:grid-cols-[1fr_18rem] lg:items-start">
            <div>
              {isLoadingBets && betSlips.length === 0 ? (
                <BetsFallback />
              ) : betsError ? (
                <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
                  {betsError}
                </p>
              ) : (
                <BetSlipList betSlips={betSlips} />
              )}
            </div>
            <aside className="lg:sticky lg:top-8">
              <BankrollSidebar data={bankroll} isLoading={bankrollLoading} error={bankrollError} />
            </aside>
          </div>
        ) : null}

        {activeTab === "sessions" ? (
          isLoadingSessions && sessions.length === 0 ? (
            <SessionsFallback />
          ) : sessionsError ? (
            <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
              {sessionsError}
            </p>
          ) : sessions.length === 0 ? (
            <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
              No agent sessions recorded yet.
            </p>
          ) : (
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
              <div className="flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
                <ul className="w-full min-w-0 space-y-1 p-2">
                  {sortedSessions.map((session) => {
                    const isSelected = session.id === selectedSessionId;
                    const PhaseIcon = sessionPhaseIcon(session.phaseId);
                    return (
                      <li key={session.id} className="w-full min-w-0">
                        <button
                          type="button"
                          onClick={() => setSelectedSessionId(session.id)}
                          className={
                            "flex w-full min-w-0 max-w-full gap-2.5 rounded-md border px-3 py-2.5 text-left transition-colors " +
                            (isSelected
                              ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                              : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                          }
                        >
                          <PhaseIcon
                            className={"mt-0.5 h-4 w-4 shrink-0 " + (isSelected ? "text-zinc-700 dark:text-zinc-300" : "text-zinc-400 dark:text-zinc-500")}
                            aria-hidden
                          />
                          <div className="min-w-0 flex-1">
                            <span className="block min-w-0 truncate font-medium text-foreground">{session.phaseName}</span>
                            <div className="mt-1 flex min-w-0 items-center gap-2">
                              <span className="min-w-0 flex-1 truncate text-left text-xs text-zinc-500 dark:text-zinc-500">
                                {formatDate(session.startedAt)}
                              </span>
                              <span className="shrink-0 text-right text-xs font-normal tabular-nums text-zinc-500 dark:text-zinc-500">
                                Session #{session.id}
                              </span>
                            </div>
                          </div>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </div>
              <div className="flex min-h-[min(70vh,36rem)] min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
                {selectedSession ? (
                  <>
                    <div className="flex min-w-0 shrink-0 items-center justify-between gap-3 border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
                      <div className="flex min-w-0 flex-1 items-center gap-3">
                        <SelectedPhaseIcon className="h-5 w-5 shrink-0 text-zinc-500 dark:text-zinc-400" aria-hidden />
                        <div className="flex min-w-0 flex-1 items-baseline justify-between gap-3">
                          <h2 className="min-w-0 flex-1 truncate text-lg font-semibold text-foreground">{selectedSession.phaseName}</h2>
                          <span className="shrink-0 whitespace-nowrap text-right text-xs font-normal text-zinc-500 dark:text-zinc-500">
                            Session #{selectedSession.id} · {formatDate(selectedSession.startedAt)}
                          </span>
                        </div>
                      </div>
                      {selectedSession.matchId != null ? (
                        <Link
                          href={`/match/${selectedSession.matchId}`}
                          className="inline-flex shrink-0 items-center gap-1 rounded-md border border-sky-300 bg-sky-500 px-3 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-sky-600 dark:border-sky-500 dark:bg-sky-600 dark:hover:bg-sky-500"
                        >
                          Match
                          <ChevronRight className="h-4 w-4 text-white/90" aria-hidden />
                        </Link>
                      ) : null}
                    </div>
                    <div className="flex min-h-0 flex-1 flex-col overflow-y-auto">
                      {isLoadingTranscript && transcriptMessages === null ? (
                        <p className="px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400">Loading transcript...</p>
                      ) : transcriptError ? (
                        <p className="px-4 py-3 text-sm text-red-800 dark:text-red-200">{transcriptError}</p>
                      ) : transcriptMessages ? (
                        <AgentSessionTranscript messages={transcriptMessages} />
                      ) : null}
                    </div>
                  </>
                ) : (
                  <div className="flex flex-1 items-center justify-center p-6 text-sm text-zinc-500 dark:text-zinc-400">
                    Select a session to view its transcript.
                  </div>
                )}
              </div>
            </div>
          )
        ) : null}

        {activeTab === "memories" ? (
          isLoadingMemories && memories.length === 0 ? (
            <MemoriesFallback />
          ) : memoriesError ? (
            <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
              {memoriesError}
            </p>
          ) : memories.length === 0 ? (
            <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
              No memories saved yet.
            </p>
          ) : (
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
              <div className="flex min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start lg:w-full">
                <ul className="min-w-0 space-y-1 p-2">
                  {sortedMemories.map((memory) => {
                    const isSelected = memory.id === selectedMemoryId;
                    return (
                      <li key={memory.id} className="min-w-0">
                        <button
                          type="button"
                          onClick={() => setSelectedMemoryId(memory.id)}
                          className={
                            "min-w-0 max-w-full rounded-md border px-3 py-2.5 text-left transition-colors " +
                            (isSelected
                              ? "border-zinc-300 bg-zinc-100 ring-2 ring-zinc-400/30 dark:border-zinc-600 dark:bg-zinc-900 dark:ring-zinc-500/30"
                              : "border-transparent hover:bg-zinc-50 dark:hover:bg-zinc-900/80")
                          }
                        >
                          <span className="line-clamp-2 min-w-0 max-w-full break-all wrap-break-word font-medium text-foreground">{memory.name}</span>
                          {memory.description ? (
                            <span className="mt-1 block min-w-0 max-w-full line-clamp-2 wrap-break-word text-sm text-zinc-600 dark:text-zinc-400">
                              {memory.description}
                            </span>
                          ) : null}
                          <span className="mt-2 block min-w-0 max-w-full truncate text-xs text-zinc-500 dark:text-zinc-500">
                            Updated {formatDate(memory.updatedAt)}
                          </span>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              </div>
              <div className="flex min-h-[min(70vh,36rem)] min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
                {selectedMemory ? (
                  <>
                    <div className="flex min-w-0 shrink-0 items-center border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
                      <div className="flex min-w-0 flex-1 items-baseline justify-between gap-3">
                        <h2 className="min-w-0 flex-1 truncate text-lg font-semibold text-foreground">{selectedMemory.name}</h2>
                        <span className="shrink-0 whitespace-nowrap text-right text-xs font-normal text-zinc-500 dark:text-zinc-500">
                          Updated {formatDate(selectedMemory.updatedAt)}
                        </span>
                      </div>
                    </div>
                    <div className="min-h-0 flex-1 overflow-y-auto px-4 py-3">
                      <pre className="wrap-break-word whitespace-pre-wrap font-mono text-sm text-zinc-800 dark:text-zinc-200">
                        {selectedMemory.content || "—"}
                      </pre>
                    </div>
                  </>
                ) : (
                  <div className="flex flex-1 items-center justify-center p-6 text-sm text-zinc-500 dark:text-zinc-400">
                    Select a memory to view its content.
                  </div>
                )}
              </div>
            </div>
          )
        ) : null}
      </main>
    </div>
  );
}
