"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Bot, ChevronRight, WalletCards } from "lucide-react";
import { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { BetSlipList } from "@/features/bets/components/bet-slip-list";
import type { BetSlipListItem } from "@/features/bets/interfaces";
import { fetchBetSlips } from "@/features/bets/services/bets-api";
import { AgentSessionTranscript } from "@/features/bets/components/agent-session-transcript";
import { fetchAgentSessionMessages, type AgentSessionMessage } from "@/features/bets/services/agent-session-api";
import { isBettingSessionPhase, sessionPhaseIcon } from "@/features/sessions/agent-session-phases";
import type { AgentSessionListItem } from "@/features/sessions/interfaces";
import { fetchAgentSessionsPage } from "@/features/sessions/services/sessions-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentSessionPhaseFilter } from "./agent-session-phase-filter";
import { AgentSessionsList } from "./agent-sessions-list";

function mergeSessions(
  existing: AgentSessionListItem[],
  incoming: AgentSessionListItem[],
): AgentSessionListItem[] {
  const seen = new Set(existing.map((session) => session.id));
  const merged = [...existing];
  for (const session of incoming) {
    if (seen.has(session.id)) continue;
    seen.add(session.id);
    merged.push(session);
  }
  return merged;
}

function formatDate(iso: string) {
    try {
        return new Date(iso).toLocaleString(undefined, {
            dateStyle: "medium",
            timeStyle: "short",
            hour12: false,
        });
    } catch {
        return iso;
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
            <div className="min-h-[min(78vh,44rem)] overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
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

export interface AgentSessionsDetailsPanelProps {
    initialSelectedSessionId?: number | null;
}

export function AgentSessionsDetailsPanel({ initialSelectedSessionId = null }: AgentSessionsDetailsPanelProps) {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();
    const [sessions, setSessions] = useState<AgentSessionListItem[]>([]);
    const [selectedSessionId, setSelectedSessionId] = useState<number | null>(null);
    const [hasMore, setHasMore] = useState(false);
    const [nextCursor, setNextCursor] = useState<{ at: string; id: number } | null>(null);
    const [isLoadingSessions, setIsLoadingSessions] = useState(true);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [sessionsError, setSessionsError] = useState<string | null>(null);
    const [loadMoreError, setLoadMoreError] = useState<string | null>(null);
    const isLoadingMoreRef = useRef(false);
    const [allBetSlips, setAllBetSlips] = useState<BetSlipListItem[]>([]);
    const [isLoadingBetSlips, setIsLoadingBetSlips] = useState(true);
    const [betSlipsError, setBetSlipsError] = useState<string | null>(null);
    const [transcriptMessages, setTranscriptMessages] = useState<AgentSessionMessage[] | null>(null);
    const [isLoadingTranscript, setIsLoadingTranscript] = useState(false);
    const [transcriptError, setTranscriptError] = useState<string | null>(null);
    const [selectedPhaseIds, setSelectedPhaseIds] = useState<number[]>([]);
    const hasActivePhaseFilter = selectedPhaseIds.length > 0;
    const phaseIdsForRequest = hasActivePhaseFilter ? selectedPhaseIds : undefined;
    const bootstrapIncludeSessionIdRef = useRef(initialSelectedSessionId);
    const lastUrlSessionIdRef = useRef(initialSelectedSessionId);
    const detailPanelRef = useRef<HTMLDivElement>(null);
    const shouldScrollToDetailRef = useRef(false);

    const applySessionsPage = useCallback(
        (page: Awaited<ReturnType<typeof fetchAgentSessionsPage>>, append: boolean) => {
            setSessions((current) => (append ? mergeSessions(current, page.items) : page.items));
            setHasMore(page.hasMore);
            setNextCursor(
                page.hasMore && page.nextCursorAt != null && page.nextCursorId != null
                    ? { at: page.nextCursorAt, id: page.nextCursorId }
                    : null,
            );
        },
        [],
    );

    useEffect(() => {
        let cancelled = false;
        setIsLoadingSessions(true);
        setSessionsError(null);
        setLoadMoreError(null);
        setSessions([]);
        setHasMore(false);
        setNextCursor(null);

        fetchAgentSessionsPage({
            includeSessionId: bootstrapIncludeSessionIdRef.current ?? undefined,
            phaseIds: phaseIdsForRequest,
        })
            .then((page) => {
                if (!cancelled) applySessionsPage(page, false);
            })
            .catch((error) => {
                if (!cancelled) {
                    setSessionsError(handleServiceError(error, "Failed to load agent sessions."));
                }
            })
            .finally(() => {
                if (!cancelled) setIsLoadingSessions(false);
            });

        return () => {
            cancelled = true;
        };
    }, [applySessionsPage, selectedPhaseIds]);

    useEffect(() => {
        if (initialSelectedSessionId === lastUrlSessionIdRef.current) return;
        lastUrlSessionIdRef.current = initialSelectedSessionId;

        if (initialSelectedSessionId == null) return;

        if (sessions.some((session) => session.id === initialSelectedSessionId)) {
            setSelectedSessionId(initialSelectedSessionId);
            return;
        }

        let cancelled = false;

        fetchAgentSessionsPage({
            includeSessionId: initialSelectedSessionId,
            phaseIds: phaseIdsForRequest,
        })
            .then((page) => {
                if (!cancelled) {
                    applySessionsPage(page, true);
                    setSelectedSessionId(initialSelectedSessionId);
                }
            })
            .catch((error) => {
                if (!cancelled) {
                    setSessionsError(handleServiceError(error, "Failed to load agent sessions."));
                }
            });

        return () => {
            cancelled = true;
        };
    }, [applySessionsPage, initialSelectedSessionId, phaseIdsForRequest, sessions]);

    const loadMore = useCallback(() => {
        if (!hasMore || !nextCursor || isLoadingMoreRef.current) return;

        isLoadingMoreRef.current = true;
        setIsLoadingMore(true);
        setLoadMoreError(null);

        fetchAgentSessionsPage({
            afterStartedAt: nextCursor.at,
            afterId: nextCursor.id,
            phaseIds: phaseIdsForRequest,
        })
            .then((page) => {
                applySessionsPage(page, true);
            })
            .catch((error) => {
                setLoadMoreError(handleServiceError(error, "Failed to load more sessions."));
            })
            .finally(() => {
                isLoadingMoreRef.current = false;
                setIsLoadingMore(false);
            });
    }, [applySessionsPage, hasMore, nextCursor, phaseIdsForRequest]);

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
            if (sessions.length === 0) return null;
            if (previous != null && sessions.some((session) => session.id === previous)) return previous;
            if (initialSelectedSessionId != null && sessions.some((session) => session.id === initialSelectedSessionId)) {
                return initialSelectedSessionId;
            }
            return sessions[0].id;
        });
    }, [sessions, initialSelectedSessionId]);

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

    useLayoutEffect(() => {
        if (!shouldScrollToDetailRef.current) return;
        shouldScrollToDetailRef.current = false;
        if (window.matchMedia("(min-width: 1024px)").matches) return;
        detailPanelRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
    }, [selectedSessionId]);

    const selectedSession = selectedSessionId != null ? sessions.find((session) => session.id === selectedSessionId) : undefined;
    const SelectedPhaseIcon = selectedSession ? sessionPhaseIcon(selectedSession.phaseId) : Bot;
    const selectedSessionSlips = useMemo(() => {
        if (selectedSessionId == null) return [];
        return allBetSlips.filter((slip) => slip.agentSessionId === selectedSessionId);
    }, [allBetSlips, selectedSessionId]);

    function handleSelectedPhaseIdsChange(phaseIds: number[]) {
        bootstrapIncludeSessionIdRef.current = selectedSessionId;
        setSelectedPhaseIds(phaseIds);
    }

    function selectSession(sessionId: number) {
        shouldScrollToDetailRef.current = true;
        setSelectedSessionId(sessionId);
        const params = new URLSearchParams(searchParams.toString());
        params.set("widget", "sessions");
        params.set("sessionId", String(sessionId));
        router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    }

    if (isLoadingSessions && sessions.length === 0) {
        return <SessionsFallback />;
    }

    if (sessionsError) {
        return <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">{sessionsError}</p>;
    }

    if (sessions.length === 0 && !hasActivePhaseFilter && !isLoadingSessions) {
        return (
            <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">No agent sessions recorded yet.</p>
        );
    }

    return (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)] lg:items-start">
            <div className="flex w-full min-w-0 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950 lg:self-start">
                <AgentSessionPhaseFilter
                    selectedPhaseIds={selectedPhaseIds}
                    onSelectedPhaseIdsChange={handleSelectedPhaseIdsChange}
                />
                <AgentSessionsList
                    sessions={sessions}
                    selectedSessionId={selectedSessionId}
                    onSelectSession={selectSession}
                    isLoading={isLoadingSessions}
                    hasMore={hasMore}
                    isLoadingMore={isLoadingMore}
                    onLoadMore={loadMore}
                    loadMoreError={loadMoreError}
                    onRetryLoadMore={loadMore}
                    emptyMessage={
                        hasActivePhaseFilter ? "No sessions match the selected types." : undefined
                    }
                />
            </div>
            <div
                ref={detailPanelRef}
                className="flex min-h-[min(78vh,44rem)] min-w-0 scroll-mt-20 flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
            >
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
                            {isBettingSessionPhase(selectedSession.phaseId) ? (
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
                                            <BetSlipList betSlips={selectedSessionSlips} groupBySession={false} showSessionLink={false} />
                                        ) : (
                                            <p className="text-sm text-zinc-500 dark:text-zinc-400">No bet slips were placed in this session.</p>
                                        )}
                                    </div>
                                </details>
                            ) : null}
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
    );
}
