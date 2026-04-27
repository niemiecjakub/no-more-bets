"use client";

import Link from "next/link";
import { Bot, ChevronRight, Globe, Lightbulb, Search, Ticket, Trash2 } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
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

/** Mirrors `AgentSessionPhase` in the API (Research=1 … MemoryCleanup=5). */
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

export default function SessionsPage() {
    const [sessions, setSessions] = useState<AgentSessionListItem[]>([]);
    const [selectedId, setSelectedId] = useState<number | null>(null);
    const [isLoadingSessions, setIsLoadingSessions] = useState(true);
    const [loadError, setLoadError] = useState<string | null>(null);

    const [transcriptMessages, setTranscriptMessages] = useState<AgentSessionMessage[] | null>(null);
    const [isLoadingTranscript, setIsLoadingTranscript] = useState(false);
    const [transcriptError, setTranscriptError] = useState<string | null>(null);

    const sortedSessions = useMemo(() => [...sessions].sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()), [sessions]);

    useEffect(() => {
        setSelectedId((prev) => {
            if (sortedSessions.length === 0) return null;
            if (prev != null && sortedSessions.some((s) => s.id === prev)) {
                return prev;
            }
            return sortedSessions[0].id;
        });
    }, [sortedSessions]);

    useEffect(() => {
        let cancelled = false;
        (async () => {
            setIsLoadingSessions(true);
            setLoadError(null);
            try {
                const data = await fetchAgentSessions();
                if (!cancelled) {
                    setSessions(data);
                }
            } catch (err) {
                if (!cancelled) {
                    setLoadError(handleServiceError(err, "Failed to load agent sessions."));
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
        if (selectedId == null) {
            setTranscriptMessages(null);
            setTranscriptError(null);
            setIsLoadingTranscript(false);
            return;
        }

        let cancelled = false;
        setTranscriptMessages(null);
        setTranscriptError(null);
        setIsLoadingTranscript(true);

        void fetchAgentSessionMessages(selectedId)
            .then((data) => {
                if (!cancelled) {
                    setTranscriptMessages(data);
                }
            })
            .catch((err) => {
                if (!cancelled) {
                    setTranscriptError(handleServiceError(err, "Failed to load session transcript."));
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
    }, [selectedId]);

    const selectedSession = selectedId != null ? sortedSessions.find((s) => s.id === selectedId) : undefined;

    const SelectedPhaseIcon = selectedSession ? sessionPhaseIcon(selectedSession.phaseId) : Bot;

    return (
        <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
            <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
                <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">Sessions</h1>
                {isLoadingSessions && sessions.length === 0 ? (
                    <SessionsFallback />
                ) : loadError ? (
                    <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">{loadError}</p>
                ) : sessions.length === 0 ? (
                    <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
                        No agent sessions recorded yet.
                    </p>
                ) : (
                    <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
                        <div className="flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
                            <ul className="w-full min-w-0 space-y-1 p-2">
                                {sortedSessions.map((s) => {
                                    const isSelected = s.id === selectedId;
                                    const PhaseIcon = sessionPhaseIcon(s.phaseId);
                                    return (
                                        <li key={s.id} className="w-full min-w-0">
                                            <button
                                                type="button"
                                                onClick={() => setSelectedId(s.id)}
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
                                                    <span className="block min-w-0 truncate font-medium text-foreground">{s.phaseName}</span>
                                                    <div className="mt-1 flex min-w-0 items-center gap-2">
                                                        <span className="min-w-0 flex-1 truncate text-left text-xs text-zinc-500 dark:text-zinc-500">
                                                            {formatDate(s.startedAt)}
                                                        </span>
                                                        <span className="shrink-0 text-right text-xs font-normal tabular-nums text-zinc-500 dark:text-zinc-500">
                                                            Session #{s.id}
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
                                                <span className="shrink-0 text-right text-xs font-normal whitespace-nowrap text-zinc-500 dark:text-zinc-500">
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
                                <div className="flex flex-1 items-center justify-center p-6 text-sm text-zinc-500 dark:text-zinc-400">Select a session to view its transcript.</div>
                            )}
                        </div>
                    </div>
                )}
            </main>
        </div>
    );
}
