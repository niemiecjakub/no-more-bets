"use client";

import Link from "next/link";
import { notFound } from "next/navigation";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import { useMatchStore } from "@/store/match-store";
import { clubLogoSlugSegment } from "../../../utils/club-logo-slug";
import { formatMatchDate } from "../../../utils/format-date";
import { handleServiceError } from "@/lib/error-handler";
import {
    MATCH_STATUS,
    type ClubLeagueStats,
    type ClubPair,
    type HeadToHead,
    type MarketPriceHistory,
    type MatchInjuriesResult,
    type MatchLineupResult,
    type RecentMatch,
    type TeamInjuriesResult,
    type TeamLineupResult,
    type TeamPerformanceResult,
    type TeamMetrics,
    type MatchDetailsSummary,
    type MatchEventDto,
} from "@/features/matches/interfaces";
import type { BetSlipSummaryDto } from "@/features/bets/interfaces";
import { LazyAgentSessionTranscript } from "@/features/bets/components/lazy-agent-session-transcript";
import { ResearchBetSlipSummary } from "@/features/bets/components/research-bet-slip-summary";
import { MatchClubEventsList } from "@/features/matches/components/match-club-events-list";
import { RecentGamesList } from "@/features/matches/components/recent-games-list";
import {
    fetchMatchAgentResearch,
    fetchMatchBettingOddsHistory,
    fetchMatchEvents,
    fetchMatchHeadToHead,
    fetchMatchInjuries,
    fetchMatchLeagueStatistics,
    fetchMatchLineups,
    fetchMatchRecentGames,
    fetchMatchResearchBetSlip,
    fetchMatchRollingPerformance,
} from "@/features/matches/services/match-insights-api";

interface MatchInsights {
    lineups: MatchLineupResult | null;
    injuries: MatchInjuriesResult | null;
    agentResearch: string | null;
    researchBetSlip: BetSlipSummaryDto | null;
    recentGames: ClubPair<RecentMatch[] | null>;
    leagueStatistics: ClubPair<ClubLeagueStats | null>;
    headToHead: HeadToHead | null;
    bettingOddsHistory: MarketPriceHistory[] | null;
    rollingPerformance: ClubPair<TeamPerformanceResult | null>;
    matchEvents: MatchEventDto[];
}

const insightKeys = [
    "lineups",
    "injuries",
    "agentResearch",
    "researchBetSlip",
    "recentGames",
    "leagueStatistics",
    "headToHead",
    "bettingOddsHistory",
    "rollingPerformance",
    "matchEvents",
] as const satisfies readonly (keyof MatchInsights)[];

type InsightKey = (typeof insightKeys)[number];

function initialInsightLoading(): Record<InsightKey, boolean> {
    return Object.fromEntries(insightKeys.map((k) => [k, true])) as Record<InsightKey, boolean>;
}

function LoadingSkeleton() {
    return (
            <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
                <div className="mb-1 h-7 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="space-y-4">
                    {[1, 2].map((i) => (
                        <div key={i} className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 overflow-hidden">
                            <div className="h-10 border-b border-zinc-200 dark:border-zinc-800 bg-zinc-100 dark:bg-zinc-900/50" />
                            <div className="h-24 px-4 py-3" />
                        </div>
                    ))}
                </div>
            </main>
    );
}

export default function MatchPage() {
    const params = useParams();
    const id = params?.id as string | undefined;
    const matchId = id != null && id !== "" ? Number(id) : NaN;
    const isValidId = !Number.isNaN(matchId) && matchId >= 1;

    const { matchAnalysisById, isLoading, error, setMatchAnalysisPage } = useMatchStore();

    const data = isValidId ? matchAnalysisById[matchId] : undefined;
    const [insights, setInsights] = useState<Partial<MatchInsights>>({});
    const [insightLoading, setInsightLoading] = useState<Record<InsightKey, boolean>>(() => initialInsightLoading());
    const [insightErrors, setInsightErrors] = useState<Partial<Record<InsightKey, string>>>({});

    useEffect(() => {
        if (!isValidId) return;
        setMatchAnalysisPage(matchId);
    }, [matchId, isValidId, setMatchAnalysisPage]);

    useEffect(() => {
        if (!isValidId) return;
        let isMounted = true;

        setInsights({});
        setInsightErrors({});
        setInsightLoading(initialInsightLoading());

        const load = <K extends InsightKey>(key: K, fetcher: () => Promise<MatchInsights[K]>) => {
            void fetcher()
                .then((value) => {
                    if (!isMounted) return;
                    setInsights((prev) => ({ ...prev, [key]: value }));
                    setInsightErrors((prev) => {
                        if (!(key in prev)) return prev;
                        const next = { ...prev };
                        delete next[key];
                        return next;
                    });
                })
                .catch((err) => {
                    if (!isMounted) return;
                    setInsightErrors((prev) => ({
                        ...prev,
                        [key]: handleServiceError(err, "Failed to load this section."),
                    }));
                })
                .finally(() => {
                    if (!isMounted) return;
                    setInsightLoading((prev) => ({ ...prev, [key]: false }));
                });
        };

        load("lineups", () => fetchMatchLineups(matchId));
        load("injuries", () => fetchMatchInjuries(matchId));
        load("agentResearch", () => fetchMatchAgentResearch(matchId));
        load("researchBetSlip", () => fetchMatchResearchBetSlip(matchId));
        load("recentGames", () => fetchMatchRecentGames(matchId));
        load("leagueStatistics", () => fetchMatchLeagueStatistics(matchId));
        load("headToHead", () => fetchMatchHeadToHead(matchId));
        load("bettingOddsHistory", () => fetchMatchBettingOddsHistory(matchId));
        load("rollingPerformance", () => fetchMatchRollingPerformance(matchId));
        load("matchEvents", () => fetchMatchEvents(matchId));

        return () => {
            isMounted = false;
        };
    }, [matchId, isValidId]);

    if (id != null && id !== "" && !isValidId) {
        notFound();
    }

    if (error?.includes("404")) {
        notFound();
    }

    if (isLoading && !data) {
        return <LoadingSkeleton />;
    }

    if (error) {
        return (
                <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
                    <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">{error}</p>
                </main>
        );
    }

    if (!data) {
        return null;
    }

    const matchDateFormatted = formatMatchDate(data.matchDate);
    const homeLogoSlug = clubLogoSlugSegment(data.homeClubSlug, data.homeClubName);
    const awayLogoSlug = clubLogoSlugSegment(data.awayClubSlug, data.awayClubName);
    const showFinishedScore = data.matchStatusId === MATCH_STATUS.Finished && data.homeGoals != null && data.awayGoals != null;
    const homeEvents = insights.matchEvents?.filter((e) => e.clubId === data.homeClubId) ?? [];
    const awayEvents = insights.matchEvents?.filter((e) => e.clubId === data.awayClubId) ?? [];
    const matchEventsLoading = insightLoading.matchEvents && insights.matchEvents === undefined;
    const matchEventsError = insightErrors.matchEvents;

    return (
            <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
                <header className="mb-6 flex flex-col items-center">
                    <p className="mb-2 text-center text-sm text-zinc-500 dark:text-zinc-400">{matchDateFormatted}</p>
                    <h1 className="w-full text-2xl font-semibold tracking-tight text-foreground">
                        <div className="grid w-full grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-3">
                            <div className="flex min-w-0 items-center justify-end gap-2.5">
                                <Link
                                    href={`/club/${data.homeClubId}`}
                                    className="min-w-0 text-balance text-end transition-colors hover:text-red-600 dark:hover:text-red-400"
                                >
                                    {data.homeClubName}
                                </Link>
                                <SlugIcon kind="club" slug={homeLogoSlug} alt={data.homeClubName} className="h-10 w-10 shrink-0" />
                            </div>
                            <span
                                className={
                                    showFinishedScore
                                        ? "inline-block min-w-[5.5rem] shrink-0 text-center text-2xl font-bold tabular-nums tracking-tight sm:text-3xl"
                                        : "shrink-0 text-center text-lg font-medium text-zinc-500 dark:text-zinc-400 sm:text-2xl sm:font-semibold"
                                }
                            >
                                {showFinishedScore ? `${data.homeGoals} - ${data.awayGoals}` : "vs"}
                            </span>
                            <div className="flex min-w-0 items-center justify-start gap-2.5">
                                <SlugIcon kind="club" slug={awayLogoSlug} alt={data.awayClubName} className="h-10 w-10 shrink-0" />
                                <Link
                                    href={`/club/${data.awayClubId}`}
                                    className="min-w-0 text-balance text-start transition-colors hover:text-red-600 dark:hover:text-red-400"
                                >
                                    {data.awayClubName}
                                </Link>
                            </div>
                        </div>
                        <div className="mt-2 grid w-full grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] gap-x-3">
                            <div className="col-start-1 min-w-0">
                                <MatchClubEventsList
                                    events={homeEvents}
                                    isLoading={matchEventsLoading}
                                    error={matchEventsError}
                                    align="end"
                                />
                            </div>
                            <span className="col-start-2 min-w-[5.5rem] shrink-0" aria-hidden />
                            <div className="col-start-3 min-w-0">
                                <MatchClubEventsList
                                    events={awayEvents}
                                    isLoading={matchEventsLoading}
                                    error={matchEventsError}
                                    align="start"
                                />
                            </div>
                        </div>
                    </h1>
                </header>

                <Card title="Agent Research" icon="🔬" className="mb-6">
                    <AgentResearchSection
                        summaryPreview={insights.agentResearch}
                        summaryLoading={insightLoading.agentResearch && insights.agentResearch === undefined}
                        summaryError={insightErrors.agentResearch}
                        researchSlip={insights.researchBetSlip}
                        researchSlipLoading={insightLoading.researchBetSlip && insights.researchBetSlip === undefined}
                        researchSlipError={insightErrors.researchBetSlip}
                        researchAgentSessionId={data.researchAgentSessionId}
                    />
                </Card>

                {data.matchDetails != null ? (
                    <Card title="Match details (Fotmob)" icon="🧾" className="mb-6">
                        <MatchDetailsSection details={data.matchDetails} />
                    </Card>
                ) : null}

                <section className="grid gap-6 2xl:grid-cols-[1.1fr_0.9fr]">
                    <div className="space-y-6">
                        <Card title="Lineups" icon="📋">
                            {insightErrors.lineups ? (
                                <InsightFieldError message={insightErrors.lineups} />
                            ) : insightLoading.lineups && insights.lineups === undefined ? (
                                <div className="px-4 py-4">
                                    <MutedText>Loading lineups...</MutedText>
                                </div>
                            ) : (
                                <TeamColumns
                                    homeClubName={data.homeClubName}
                                    awayClubName={data.awayClubName}
                                    homeLogoSlug={homeLogoSlug}
                                    awayLogoSlug={awayLogoSlug}
                                    home={<LineupList lineup={insights.lineups?.home} />}
                                    away={<LineupList lineup={insights.lineups?.away} />}
                                />
                            )}
                        </Card>

                        <Card title="Injuries / Unavailable players" icon="🏥">
                            {insightErrors.injuries ? (
                                <InsightFieldError message={insightErrors.injuries} />
                            ) : insightLoading.injuries && insights.injuries === undefined ? (
                                <div className="px-4 py-4">
                                    <MutedText>Loading injuries...</MutedText>
                                </div>
                            ) : (
                                <TeamColumns
                                    homeClubName={data.homeClubName}
                                    awayClubName={data.awayClubName}
                                    homeLogoSlug={homeLogoSlug}
                                    awayLogoSlug={awayLogoSlug}
                                    home={<InjuriesList injuries={insights.injuries?.home} />}
                                    away={<InjuriesList injuries={insights.injuries?.away} />}
                                />
                            )}
                        </Card>

                        <Card title="Recent league games per club" icon="🕒">
                            {insightErrors.recentGames ? (
                                <InsightFieldError message={insightErrors.recentGames} />
                            ) : insightLoading.recentGames && insights.recentGames === undefined ? (
                                <div className="px-4 py-4">
                                    <MutedText>Loading recent games...</MutedText>
                                </div>
                            ) : (
                                <TeamColumns
                                    homeClubName={data.homeClubName}
                                    awayClubName={data.awayClubName}
                                    homeLogoSlug={homeLogoSlug}
                                    awayLogoSlug={awayLogoSlug}
                                    home={<RecentGamesList games={insights.recentGames?.home} showLeagueNote />}
                                    away={<RecentGamesList games={insights.recentGames?.away} showLeagueNote />}
                                />
                            )}
                        </Card>

                        <Card title="Rolling performance" icon="📈">
                            {insightErrors.rollingPerformance ? (
                                <InsightFieldError message={insightErrors.rollingPerformance} />
                            ) : insightLoading.rollingPerformance && insights.rollingPerformance === undefined ? (
                                <div className="px-4 py-4">
                                    <MutedText>Loading rolling performance...</MutedText>
                                </div>
                            ) : (
                                <TeamColumns
                                    homeClubName={data.homeClubName}
                                    awayClubName={data.awayClubName}
                                    homeLogoSlug={homeLogoSlug}
                                    awayLogoSlug={awayLogoSlug}
                                    home={<RollingPerformanceSection data={insights.rollingPerformance?.home} />}
                                    away={<RollingPerformanceSection data={insights.rollingPerformance?.away} />}
                                />
                            )}
                        </Card>
                    </div>

                    <div className="space-y-6">
                        <Card title="League statistics" icon="🏆">
                            {insightErrors.leagueStatistics ? (
                                <InsightFieldError message={insightErrors.leagueStatistics} />
                            ) : insightLoading.leagueStatistics && insights.leagueStatistics === undefined ? (
                                <div className="px-4 py-4">
                                    <MutedText>Loading league statistics...</MutedText>
                                </div>
                            ) : (
                                <TeamColumns
                                    homeClubName={data.homeClubName}
                                    awayClubName={data.awayClubName}
                                    homeLogoSlug={homeLogoSlug}
                                    awayLogoSlug={awayLogoSlug}
                                    home={<LeagueStatsSection stats={insights.leagueStatistics?.home} />}
                                    away={<LeagueStatsSection stats={insights.leagueStatistics?.away} />}
                                />
                            )}
                        </Card>

                        <Card title="Head-to-head" icon="⚔️">
                            <HeadToHeadSection
                                data={insights.headToHead}
                                isLoading={insightLoading.headToHead && insights.headToHead === undefined}
                                error={insightErrors.headToHead}
                                homeLogoSlug={homeLogoSlug}
                                awayLogoSlug={awayLogoSlug}
                            />
                        </Card>

                        <Card title="Betting odds movement / history" icon="💹">
                            <BettingOddsSection
                                data={insights.bettingOddsHistory}
                                isLoading={insightLoading.bettingOddsHistory && insights.bettingOddsHistory === undefined}
                                error={insightErrors.bettingOddsHistory}
                            />
                        </Card>
                    </div>
                </section>
            </main>
    );
}

interface CardProps {
    title: string;
    icon: string;
    children: React.ReactNode;
    className?: string;
}

function Card({ title, icon, children, className }: CardProps) {
    return (
        <section className={`overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950${className ? ` ${className}` : ""}`}>
            <details open className="group">
                <summary className="flex cursor-pointer list-none items-center justify-between border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
                    <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
                        <span aria-hidden>{icon}</span>
                        <span>{title}</span>
                    </h2>
                    <span className="text-sm text-zinc-500 transition-transform group-open:rotate-180 dark:text-zinc-400">▼</span>
                </summary>
                <div>{children}</div>
            </details>
        </section>
    );
}

function MutedText({ children }: { children: React.ReactNode }) {
    return <p className="text-sm text-zinc-500 dark:text-zinc-400">{children}</p>;
}

function InsightFieldError({ message }: { message: string }) {
    return (
        <div className="px-4 py-4">
            <p className="text-sm text-red-800 dark:text-red-200">{message}</p>
        </div>
    );
}

interface TeamColumnsProps {
    homeClubName: string;
    awayClubName: string;
    homeLogoSlug: string;
    awayLogoSlug: string;
    home: React.ReactNode;
    away: React.ReactNode;
}

function TeamColumns({ homeClubName, awayClubName, homeLogoSlug, awayLogoSlug, home, away }: TeamColumnsProps) {
    return (
        <div className="grid divide-y divide-zinc-200 dark:divide-zinc-800 md:grid-cols-2 md:divide-x md:divide-y-0">
            <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
                <div className="mb-3 flex min-w-0 items-center gap-2.5">
                    <SlugIcon kind="club" slug={homeLogoSlug} alt={homeClubName} className="h-7 w-7" />
                    <h3 className="min-w-0 truncate text-sm font-semibold text-foreground">{homeClubName}</h3>
                </div>
                {home}
            </div>
            <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
                <div className="mb-3 flex min-w-0 items-center gap-2.5">
                    <SlugIcon kind="club" slug={awayLogoSlug} alt={awayClubName} className="h-7 w-7" />
                    <h3 className="min-w-0 truncate text-sm font-semibold text-foreground">{awayClubName}</h3>
                </div>
                {away}
            </div>
        </div>
    );
}

function LineupList({ lineup }: { lineup?: TeamLineupResult }) {
    if (!lineup) return <MutedText>No lineup data.</MutedText>;
    return (
        <div>
            <p className="mb-2 text-xs uppercase tracking-wide text-zinc-500 dark:text-zinc-400">{lineup.lineupType}</p>
            {lineup.players.length === 0 ? (
                <MutedText>No players listed.</MutedText>
            ) : (
                <ul className="flex flex-col gap-1 text-sm">
                    {lineup.players.map((player) => (
                        <li key={`${player.name}-${player.position}`} className="flex items-center justify-between gap-3">
                            <span className="truncate text-foreground">{player.name}</span>
                            <span className="shrink-0 text-xs text-zinc-500 dark:text-zinc-400">{player.position}</span>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}

function InjuriesList({ injuries }: { injuries?: TeamInjuriesResult }) {
    if (!injuries) return <MutedText>No injury data.</MutedText>;
    if (injuries.injuries.length === 0) return <MutedText>No reported injuries.</MutedText>;
    return (
        <ul className="flex flex-col gap-2 text-sm">
            {injuries.injuries.map((player) => (
                <li key={`${player.name}-${player.position}`} className="rounded-md border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-950">
                    <p className="font-medium text-foreground">{player.name}</p>
                    <p className="text-xs text-zinc-500 dark:text-zinc-400">
                        {player.position} · {player.injuryStatus}
                    </p>
                </li>
            ))}
        </ul>
    );
}


function LeagueStatsSection({ stats }: { stats?: ClubLeagueStats | null }) {
    if (!stats) return <MutedText>No league statistics.</MutedText>;
    const rows = [
        ["Position", stats.position],
        ["Points", stats.points],
        ["Wins / Draws / Losses", `${stats.wins} / ${stats.draws} / ${stats.losses}`],
        ["Goals For / Against", `${stats.goalsFor} / ${stats.goalsAgainst}`],
        ["xG / xGA", `${stats.xg.toFixed(2)} / ${stats.xga.toFixed(2)}`],
        ["xPts", stats.xpts.toFixed(2)],
    ];
    return (
        <ul className="flex flex-col gap-1 text-sm">
            {rows.map(([label, value]) => (
                <li key={label} className="flex items-start justify-between gap-3">
                    <span className="text-zinc-500 dark:text-zinc-400">{label}</span>
                    <span className="font-medium text-foreground">{value}</span>
                </li>
            ))}
        </ul>
    );
}

function HeadToHeadSection({ data, isLoading, error, homeLogoSlug, awayLogoSlug }: { data?: HeadToHead | null; isLoading: boolean; error?: string; homeLogoSlug: string; awayLogoSlug: string }) {
    if (error) {
        return <InsightFieldError message={error} />;
    }
    if (isLoading && data === undefined) {
        return (
            <div className="px-4 py-4">
                <MutedText>Loading head-to-head...</MutedText>
            </div>
        );
    }
    if (!data) {
        return (
            <div className="px-4 py-4">
                <MutedText>No head-to-head data available.</MutedText>
            </div>
        );
    }
    return (
        <div>
            <p className="border-b border-zinc-200 px-4 py-3 text-xs text-zinc-500 dark:border-zinc-800 dark:text-zinc-400">All-time H2H.</p>
            <div className="grid divide-y divide-zinc-200 dark:divide-zinc-800 md:grid-cols-2 md:divide-x md:divide-y-0">
                <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
                    <div className="mb-3 flex min-w-0 items-center gap-2.5">
                        <SlugIcon kind="club" slug={homeLogoSlug} alt={data.teamA.name} className="h-7 w-7" />
                        <h3 className="min-w-0 truncate text-sm font-semibold text-foreground">{data.teamA.name}</h3>
                    </div>
                    <HeadToHeadMetricsList team={data.teamA} />
                </div>
                <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
                    <div className="mb-3 flex min-w-0 items-center gap-2.5">
                        <SlugIcon kind="club" slug={awayLogoSlug} alt={data.teamB.name} className="h-7 w-7" />
                        <h3 className="min-w-0 truncate text-sm font-semibold text-foreground">{data.teamB.name}</h3>
                    </div>
                    <HeadToHeadMetricsList team={data.teamB} />
                </div>
            </div>
            <p className="border-t border-zinc-200 px-4 py-3 text-xs text-zinc-500 dark:border-zinc-800 dark:text-zinc-400">
                Total matches: {data.totalMatches} · Draws: {data.totalDraws}
            </p>
        </div>
    );
}

function HeadToHeadMetricsList({ team }: { team: TeamMetrics }) {
    const rows: [string, string | number][] = [
        ["Wins", team.totalWins],
        ["Goals", team.totalGoalsScored],
        ["Conceded", team.totalGoalsConceded],
        ["Win %", `${team.winPercentage.toFixed(1)}%`],
    ];
    return (
        <ul className="flex flex-col gap-1 text-sm">
            {rows.map(([label, value]) => (
                <li key={label} className="flex justify-between gap-2">
                    <span className="text-zinc-500 dark:text-zinc-400">{label}</span>
                    <span className="font-medium text-foreground">{value}</span>
                </li>
            ))}
        </ul>
    );
}

function BettingOddsSection({ data, isLoading, error }: { data?: MarketPriceHistory[] | null; isLoading: boolean; error?: string }) {
    if (error) {
        return <InsightFieldError message={error} />;
    }
    if (isLoading && data === undefined) {
        return (
            <div className="px-4 py-4">
                <MutedText>Loading odds history...</MutedText>
            </div>
        );
    }
    if (!data || data.length === 0) {
        return (
            <div className="px-4 py-4">
                <MutedText>No betting odds history.</MutedText>
            </div>
        );
    }
    return (
        <div className="divide-y divide-zinc-200 dark:divide-zinc-800">
            {data.map((market) => (
                <div key={market.marketKey} className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
                    <h4 className="mb-3 text-sm font-semibold text-foreground">{market.marketDisplayName ?? market.marketKey}</h4>
                    <ul className="flex flex-col gap-2 text-sm">
                        {market.outcomes.map((outcome) => (
                            <li key={outcome.outcomeName} className="flex justify-between gap-3">
                                <span className="text-zinc-600 dark:text-zinc-300">{outcome.outcomeName}</span>
                                <span className="shrink-0 font-medium tabular-nums text-foreground">
                                    {outcome.timeline.length === 0 ? "No data" : `${outcome.timeline[0].price.toFixed(2)} -> ${outcome.timeline[outcome.timeline.length - 1].price.toFixed(2)}`}
                                </span>
                            </li>
                        ))}
                    </ul>
                </div>
            ))}
        </div>
    );
}

function RollingPerformanceSection({ data }: { data?: TeamPerformanceResult | null }) {
    if (!data) return <MutedText>No rolling performance data.</MutedText>;
    return (
        <div className="space-y-3 text-sm">
            <div className="flex justify-between gap-3">
                <span className="text-zinc-500 dark:text-zinc-400">Avg team rating</span>
                <span className="font-semibold text-foreground">{data.avgTeamRating.toFixed(2)}</span>
            </div>
            <p className="text-xs text-zinc-500 dark:text-zinc-400">Formations: {data.formations.join(", ") || "N/A"}</p>
            {data.topPlayers.length === 0 ? (
                <MutedText>No player ratings available.</MutedText>
            ) : (
                <ul className="flex flex-col gap-2">
                    {data.topPlayers.slice(0, 5).map((player) => (
                        <li key={player.player} className="flex items-center justify-between gap-2 rounded-md border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-950">
                            <span className="truncate text-foreground">{player.player}</span>
                            <span className="shrink-0 font-medium text-foreground">{player.avgRating.toFixed(2)}</span>
                        </li>
                    ))}
                </ul>
            )}
            {data.matches && data.matches.length > 0 ? (
                <div className="space-y-2 border-t border-zinc-200 pt-3 dark:border-zinc-800">
                    <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Match stats used in calculations</p>
                    <ul className="flex flex-col gap-2">
                        {data.matches.map((match) => (
                            <li key={`${match.matchId}-${match.date}-${match.opponent}`} className="rounded-md border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-950">
                                <div className="flex flex-wrap items-center justify-between gap-2">
                                    {match.matchId > 0 ? (
                                        <Link href={`/match/${match.matchId}`} className="font-medium text-foreground underline-offset-2 hover:underline">
                                            vs {match.opponent}
                                        </Link>
                                    ) : (
                                        <span className="font-medium text-foreground">vs {match.opponent}</span>
                                    )}
                                    <span className="text-xs text-zinc-500 dark:text-zinc-400">{match.date}</span>
                                </div>
                                <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-zinc-600 dark:text-zinc-300">
                                    <span>Team rating: {match.teamRating != null ? match.teamRating.toFixed(2) : "N/A"}</span>
                                    <span>·</span>
                                    <span>Formation: {match.formation || "N/A"}</span>
                                </div>
                                <div className="mt-2">
                                    {match.playerRatings.length === 0 ? (
                                        <p className="text-xs text-zinc-500 dark:text-zinc-400">No player ratings.</p>
                                    ) : (
                                        <div className="flex flex-wrap gap-1.5">
                                            {match.playerRatings.slice(0, 8).map((player) => (
                                                <span
                                                    key={`${match.matchId}-${player.player}`}
                                                    className="inline-flex items-center rounded-md bg-zinc-100 px-2 py-0.5 text-xs text-zinc-700 dark:bg-zinc-900 dark:text-zinc-300"
                                                >
                                                    {player.player}: {player.rating.toFixed(2)}
                                                </span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </li>
                        ))}
                    </ul>
                </div>
            ) : null}
        </div>
    );
}

function PreviewSection({
    preview,
    isLoading,
    error,
    loadingMessage = "Loading preview...",
    emptyMessage = "No preview available.",
}: {
    preview?: string | null;
    isLoading: boolean;
    error?: string;
    loadingMessage?: string;
    emptyMessage?: string;
}) {
    if (error) return <p className="text-sm text-red-800 dark:text-red-200">{error}</p>;
    if (isLoading) return <MutedText>{loadingMessage}</MutedText>;
    if (preview == null || preview === "") return <MutedText>{emptyMessage}</MutedText>;
    return <p className="whitespace-pre-wrap text-sm leading-6 text-zinc-700 dark:text-zinc-300">{preview}</p>;
}

function AgentResearchSection({
    summaryPreview,
    summaryLoading,
    summaryError,
    researchSlip,
    researchSlipLoading,
    researchSlipError,
    researchAgentSessionId,
}: {
    summaryPreview?: string | null;
    summaryLoading: boolean;
    summaryError?: string;
    researchSlip?: BetSlipSummaryDto | null;
    researchSlipLoading: boolean;
    researchSlipError?: string;
    researchAgentSessionId: number | null;
}) {
    const [transcriptOpen, setTranscriptOpen] = useState(false);
    const [internalProcessOpen, setInternalProcessOpen] = useState(false);

    const showResearchBetSlipBlock = !researchSlipLoading && (researchSlip != null || researchSlipError != null);

    return (
        <div className="px-4 py-4">
            <PreviewSection preview={summaryPreview} isLoading={summaryLoading} error={summaryError} loadingMessage="Loading agent research..." emptyMessage="No agent research available." />
            {showResearchBetSlipBlock ? (
                <div className="mt-6 border-t border-zinc-200 pt-6 dark:border-zinc-800">
                    <h3 className="mb-3 text-sm font-semibold text-foreground">Research bet slip</h3>
                    <p className="mb-3 text-xs text-zinc-500 dark:text-zinc-400">Fictional paper slip from the research agent (not bankroll-backed).</p>
                    <ResearchBetSlipSummary slip={researchSlip ?? null} isLoading={false} error={researchSlipError} variant="matchPage" />
                </div>
            ) : null}
            {researchAgentSessionId != null ? (
                <details
                    className="group mt-4 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800"
                    onToggle={(e) => {
                        const el = e.currentTarget;
                        if (el.open) setTranscriptOpen(true);
                        setInternalProcessOpen(el.open);
                    }}
                >
                    <summary className="cursor-pointer list-none bg-zinc-50 px-3 py-2 text-sm font-medium text-foreground hover:bg-zinc-100 dark:bg-zinc-900/50 dark:hover:bg-zinc-900">
                        <span className="inline-flex w-full items-center justify-between gap-2">
                            <span>Internal process</span>
                            <span className="text-xs text-zinc-500 dark:text-zinc-400">{internalProcessOpen ? "▲" : "▼"}</span>
                        </span>
                    </summary>
                    <div className="border-t border-zinc-200 dark:border-zinc-800">
                        <LazyAgentSessionTranscript sessionId={researchAgentSessionId} active={transcriptOpen} />
                    </div>
                </details>
            ) : null}
        </div>
    );
}

function MatchDetailsSection({ details }: { details: MatchDetailsSummary }) {
    const hasReview = details.fotmobReview != null && details.fotmobReview !== "";
    const hasUrl = details.fotmobUrl != null && details.fotmobUrl !== "";
    const hasPayload = details.fotmobDetailsJson != null && details.fotmobDetailsJson !== "";

    if (!hasReview && !hasUrl && !hasPayload) {
        return (
            <div className="px-4 py-4">
                <MutedText>No match details available.</MutedText>
            </div>
        );
    }

    return (
        <div className="space-y-4 px-4 py-4">
            {hasReview ? (
                <div className="space-y-1">
                    <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Review</p>
                    <p className="whitespace-pre-wrap text-sm leading-6 text-zinc-700 dark:text-zinc-300">{details.fotmobReview}</p>
                </div>
            ) : null}

            {hasUrl ? (
                <div className="space-y-1">
                    <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">Source</p>
                    <a href={details.fotmobUrl ?? "#"} target="_blank" rel="noreferrer" className="text-sm text-blue-600 underline-offset-2 hover:underline dark:text-blue-400">
                        Open Fotmob match page
                    </a>
                </div>
            ) : null}

            {hasPayload ? (
                <details className="rounded-md border border-zinc-200 bg-zinc-50 p-2 dark:border-zinc-800 dark:bg-zinc-900/35">
                    <summary className="cursor-pointer text-sm font-medium text-foreground">Raw match payload</summary>
                    <pre className="mt-2 max-h-72 overflow-auto whitespace-pre-wrap wrap-break-word text-xs leading-5 text-zinc-700 dark:text-zinc-300">{details.fotmobDetailsJson}</pre>
                </details>
            ) : null}
        </div>
    );
}
