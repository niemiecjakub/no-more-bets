"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Bot, ChevronRight, Globe, Lightbulb, Search, Ticket, Trash2, WalletCards } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";
import type { BetSlipListItem } from "@/features/bets/interfaces";
import { fetchBetSlips } from "@/features/bets/services/bets-api";
import { AgentSessionTranscript } from "@/features/bets/components/agent-session-transcript";
import { fetchAgentSessionMessages, type AgentSessionMessage } from "@/features/bets/services/agent-session-api";
import type { AgentSessionListItem } from "@/features/sessions/interfaces";
import { fetchAgentSessions } from "@/features/sessions/services/sessions-api";
import { handleServiceError } from "@/lib/error-handler";

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

export interface AgentDashboardSessionsDetailsPanelProps {
  initialSelectedSessionId?: number | null;
}

export function AgentDashboardSessionsDetailsPanel({
  initialSelectedSessionId = null,
}: AgentDashboardSessionsDetailsPanelProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [sessions, setSessions] = useState<AgentSessionListItem[]>([]);
  const [selectedSessionId, setSelectedSessionId] = useState<number | null>(null);
  const [isLoadingSessions, setIsLoadingSessions] = useState(true);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [allBetSlips, setAllBetSlips] = useState<BetSlipListItem[]>([]);
  const [isLoadingBetSlips, setIsLoadingBetSlips] = useState(true);
  const [betSlipsError, setBetSlipsError] = useState<string | null>(null);
  const [transcriptMessages, setTranscriptMessages] = useState<AgentSessionMessage[] | null>(null);
  const [isLoadingTranscript, setIsLoadingTranscript] = useState(false);
  const [transcriptError, setTranscriptError] = useState<string | null>(null);

  const sortedSessions = useMemo(
    () => [...sessions].sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()),
    [sessions]
  );

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
    setIsLoadingBetSlips(true);
    setBetSlipsError(null);

    fetchBetSlips()
      .then((data) => {
        if (!cancelled) setAllBetSlips(data);
      })
      .catch((error) => {
        if (!cancelled) {
          setBetSlipsError(handleServiceError(error, "Failed to load session bet slips."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoadingBetSlips(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setSelectedSessionId((previous) => {
      if (sortedSessions.length === 0) return null;
      if (previous != null && sortedSessions.some((session) => session.id === previous)) return previous;
      if (
        initialSelectedSessionId != null &&
        sortedSessions.some((session) => session.id === initialSelectedSessionId)
      ) {
        return initialSelectedSessionId;
      }
      return sortedSessions[0].id;
    });
  }, [sortedSessions, initialSelectedSessionId]);

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
  const SelectedPhaseIcon = selectedSession ? sessionPhaseIcon(selectedSession.phaseId) : Bot;
  const selectedSessionSlips = useMemo(() => {
    if (selectedSessionId == null) return [];
    return allBetSlips.filter((slip) => slip.agentSessionId === selectedSessionId);
  }, [allBetSlips, selectedSessionId]);

  function selectSession(sessionId: number) {
    setSelectedSessionId(sessionId);
    const params = new URLSearchParams(searchParams.toString());
    params.set("widget", "sessions");
    params.set("sessionId", String(sessionId));
    router.replace(`${pathname}?${params.toString()}`);
  }

  if (isLoadingSessions && sessions.length === 0) {
    return <SessionsFallback />;
  }

  if (sessionsError) {
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {sessionsError}
      </p>
    );
  }

  if (sessions.length === 0) {
    return (
      <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
        No agent sessions recorded yet.
      </p>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
      <div className="flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
        <div className="h-full max-h-[min(70vh,36rem)] overflow-y-auto [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600">
          <ul className="w-full min-w-0 space-y-1 p-2">
            {sortedSessions.map((session) => {
              const isSelected = session.id === selectedSessionId;
              const PhaseIcon = sessionPhaseIcon(session.phaseId);
              return (
                <li key={session.id} className="w-full min-w-0">
                  <button
                    type="button"
                    onClick={() => selectSession(session.id)}
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
              <details className="group border-b border-violet-200 bg-violet-50/80 dark:border-violet-900/60 dark:bg-violet-950/30">
                <summary className="cursor-pointer list-none px-4 py-3 transition-colors hover:bg-zinc-100/90 dark:hover:bg-zinc-800/80">
                  <span className="inline-flex w-full items-center justify-between gap-3">
                    <span className="inline-flex items-center gap-2 text-sm font-semibold text-zinc-900 dark:text-zinc-100">
                      <WalletCards className="h-4 w-4 text-violet-600 dark:text-violet-400" aria-hidden />
                      See bets placed in this session
                      <span className="inline-flex items-center rounded-md border border-violet-300 bg-violet-100 px-1.5 py-0.5 text-xs font-semibold text-violet-700 dark:border-violet-500/50 dark:bg-violet-950/50 dark:text-violet-300">
                        {selectedSessionSlips.length}
                      </span>
                    </span>
                    <span className="text-xs font-medium text-zinc-600 transition-transform group-open:rotate-180 dark:text-zinc-300">▼</span>
                  </span>
                </summary>
                <div className="border-t border-violet-200/80 bg-white/70 px-4 py-3 dark:border-violet-900/60 dark:bg-zinc-950/70">
                  {isLoadingBetSlips ? (
                    <p className="text-sm text-zinc-500 dark:text-zinc-400">Loading bet slips...</p>
                  ) : betSlipsError ? (
                    <p className="text-sm text-red-800 dark:text-red-200">{betSlipsError}</p>
                  ) : selectedSessionSlips.length > 0 ? (
                    <BetSlipList
                      betSlips={selectedSessionSlips}
                      groupBySession={false}
                      showSessionLink={false}
                    />
                  ) : (
                    <p className="text-sm text-zinc-500 dark:text-zinc-400">
                      No bet slips were placed in this session.
                    </p>
                  )}
                </div>
              </details>
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
  );
}
