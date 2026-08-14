"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { MatchList } from "../../features/matches/components/match-list";
import { ALL_STATUSES_ID, MatchFiltersPanel, parseSortOrderParam, statusFilters } from "../../features/matches/components/match-filters-panel";
import { MATCH_STATUS } from "../../features/matches/interfaces";
import { getDefaultSortForStatus, type MatchDateSortOrder } from "../../features/matches/services/matches-api";
import { MatchFiltersMobileSheet } from "../../features/matches/components/match-filters-mobile-sheet";
import { useMatchStore } from "@/store/match-store";
import { useLeagueStore } from "@/store/league-store";
import { fetchSeasonYears } from "@/features/leagues/services/leagues-api";
import { fetchAgentDashboardResearchBettingSummaryWidget } from "@/features/bets/services/research-dashboard-api";
import type { AgentDashboardResearchBettingSummaryWidget } from "@/features/bets/interfaces";
import { ResearchBettingPanel } from "@/features/bets/components/research-betting-panel";
import { ResearchBettingMobileSheet } from "@/features/bets/components/research-betting-mobile-sheet";
import { handleServiceError } from "@/lib/error-handler";
import { useRevealOnScrollUp } from "@/hooks/use-reveal-on-scroll-up";

function MatchesFallback() {
    return (
        <div className="animate-pulse space-y-5">
            {[1, 2].map((group) => (
                <section key={group} className="space-y-2">
                    <div className="h-4 w-56 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
                        {[1, 2, 3].map((row) => (
                            <div key={`${group}-${row}`} className="space-y-2 border-b border-zinc-200 bg-white px-4 py-3 last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950">
                                <div className="mx-auto h-3 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
                                <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-3">
                                    <div className="ml-auto flex items-center gap-2">
                                        <div className="h-6 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                                        <div className="h-7 w-7 rounded-full bg-zinc-200 dark:bg-zinc-800" />
                                    </div>
                                    <div className="h-6 w-14 rounded bg-zinc-200 dark:bg-zinc-800" />
                                    <div className="flex items-center gap-2">
                                        <div className="h-7 w-7 rounded-full bg-zinc-200 dark:bg-zinc-800" />
                                        <div className="h-6 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                                    </div>
                                </div>
                                <div className="flex flex-wrap items-center justify-center gap-1.5 pt-0.5">
                                    {[1, 2, 3, 4].map((chip) => (
                                        <div key={chip} className="h-5 w-14 rounded-md bg-zinc-200 dark:bg-zinc-800" />
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                </section>
            ))}
        </div>
    );
}

export default function HomePage() {
    const router = useRouter();
    const pathname = usePathname();
    const searchParams = useSearchParams();
    const { matches, isLoading, error, hasMore, isLoadingMore, loadMoreError, setMatches, loadMoreMatches, retryLoadMore } = useMatchStore();

    const handleLoadMoreMatches = useCallback(() => {
        void loadMoreMatches();
    }, [loadMoreMatches]);

    const handleRetryLoadMoreMatches = useCallback(() => {
        retryLoadMore();
    }, [retryLoadMore]);
    const { leagues, isLoading: isLeaguesLoading, error: leaguesError, setLeagues } = useLeagueStore();
    const [seasonYears, setSeasonYears] = useState<string[]>([]);
    const [isSeasonYearsLoading, setIsSeasonYearsLoading] = useState(true);
    const [seasonYearsError, setSeasonYearsError] = useState<string | null>(null);
    const [summaryWidget, setSummaryWidget] = useState<AgentDashboardResearchBettingSummaryWidget | null>(null);
    const [isStatsLoading, setIsStatsLoading] = useState(false);
    const [statsError, setStatsError] = useState<string | null>(null);
    const isMobileChromeVisible = useRevealOnScrollUp();

    const latestSeasonYear = seasonYears[0] ?? null;

    const { selectedLeagueIds, selectedStatusId, selectedSortOrder, searchQuery, selectedSeasonYears, matchFilters, seasonFilterReady } = useMemo(() => {
        const statusParam = Number(searchParams.get("status"));
        const matchedStatus = statusFilters.find((statusFilter) => statusFilter.id === statusParam);
        const parsedStatusId = matchedStatus?.id ?? MATCH_STATUS.Upcoming;
        const parsedSortOrder = parseSortOrderParam(searchParams.get("sort"), parsedStatusId);
        const parsedSearchQuery = (searchParams.get("search") ?? "").trim();

        const leaguesParam = searchParams.get("leagues");
        const parsedLeagueIds = leaguesParam
            ? leaguesParam
                  .split(",")
                  .map((item) => Number(item.trim()))
                  .filter((id) => Number.isInteger(id) && id > 0)
            : [];

        const seasonRaw = searchParams.get("season");
        let parsedSeasonYears: string[];
        if (seasonRaw === null) {
            parsedSeasonYears = latestSeasonYear ? [latestSeasonYear] : [];
        } else if (seasonRaw.trim() === "") {
            parsedSeasonYears = [];
        } else {
            parsedSeasonYears = seasonRaw
                .split(",")
                .map((item) => item.trim())
                .filter((year) => seasonYears.includes(year));
            if (parsedSeasonYears.length === 0 && latestSeasonYear) {
                parsedSeasonYears = [latestSeasonYear];
            }
        }

        const seasonFilterReady = seasonRaw !== null || latestSeasonYear != null;

        return {
            selectedLeagueIds: parsedLeagueIds,
            selectedStatusId: parsedStatusId,
            selectedSortOrder: parsedSortOrder,
            searchQuery: parsedSearchQuery,
            selectedSeasonYears: parsedSeasonYears,
            seasonFilterReady,
            matchFilters: {
                matchStatusId: parsedStatusId === ALL_STATUSES_ID ? undefined : parsedStatusId,
                leagueIds: parsedLeagueIds.length > 0 ? parsedLeagueIds : undefined,
                sortOrder: parsedSortOrder,
                search: parsedSearchQuery || undefined,
                seasonYears: parsedSeasonYears.length > 0 ? parsedSeasonYears : undefined,
            },
        };
    }, [searchParams, seasonYears, latestSeasonYear]);

    const researchStatsScopeLabel = useMemo(() => {
        const leaguesLabel =
            selectedLeagueIds.length === 0
                ? "All leagues"
                : (() => {
                      const selectedNames = leagues.filter((league) => selectedLeagueIds.includes(league.id)).map((league) => league.name);
                      return selectedNames.length === 0 ? "Selected leagues" : selectedNames.join(", ");
                  })();

        const seasonsLabel =
            selectedSeasonYears.length === 0
                ? "All seasons"
                : selectedSeasonYears.length === 1
                  ? selectedSeasonYears[0]
                  : selectedSeasonYears.length === 2
                    ? selectedSeasonYears.join(", ")
                    : `${selectedSeasonYears.length} seasons`;

        return `${leaguesLabel} · ${seasonsLabel}`;
    }, [leagues, selectedLeagueIds, selectedSeasonYears]);

    useEffect(() => {
        setLeagues();
    }, [setLeagues]);

    useEffect(() => {
        let isMounted = true;

        async function loadSeasonYears() {
            setIsSeasonYearsLoading(true);
            setSeasonYearsError(null);
            try {
                const items = await fetchSeasonYears();
                if (!isMounted) return;
                setSeasonYears(items.map((item) => item.year));
            } catch (err) {
                if (!isMounted) return;
                setSeasonYearsError(handleServiceError(err, "Failed to load seasons."));
            } finally {
                if (isMounted) setIsSeasonYearsLoading(false);
            }
        }

        void loadSeasonYears();
        return () => {
            isMounted = false;
        };
    }, []);

    useEffect(() => {
        if (!seasonFilterReady) return;
        setMatches(matchFilters);
    }, [matchFilters, setMatches, seasonFilterReady]);

    useEffect(() => {
        if (!seasonFilterReady) return;

        let isMounted = true;

        async function loadStats() {
            setIsStatsLoading(true);
            setStatsError(null);
            try {
                const summary = await fetchAgentDashboardResearchBettingSummaryWidget(selectedLeagueIds, selectedSeasonYears);
                if (!isMounted) return;
                setSummaryWidget(summary);
            } catch (err) {
                if (!isMounted) return;
                setStatsError(handleServiceError(err, "Failed to load betting stats."));
            } finally {
                if (isMounted) setIsStatsLoading(false);
            }
        }

        void loadStats();
        return () => {
            isMounted = false;
        };
    }, [selectedLeagueIds, selectedSeasonYears, seasonFilterReady]);

    function syncFiltersInUrl(nextLeagueIds: number[], nextStatusId: number, nextSortOrder?: MatchDateSortOrder, nextSeasonYears?: string[]) {
        const params = new URLSearchParams(searchParams.toString());
        params.set("status", String(nextStatusId));
        const sortOrder = nextSortOrder ?? getDefaultSortForStatus(nextStatusId);
        if (sortOrder === getDefaultSortForStatus(nextStatusId)) {
            params.delete("sort");
        } else {
            params.set("sort", sortOrder);
        }
        if (nextLeagueIds.length > 0) {
            params.set("leagues", nextLeagueIds.join(","));
        } else {
            params.delete("leagues");
        }
        // Absent season = latest (implicit); empty = all seasons; otherwise explicit.
        const seasonYearsForUrl = nextSeasonYears ?? selectedSeasonYears;
        const isLatestOnlyDefault = latestSeasonYear != null && seasonYearsForUrl.length === 1 && seasonYearsForUrl[0] === latestSeasonYear;
        if (isLatestOnlyDefault) {
            params.delete("season");
        } else {
            params.set("season", seasonYearsForUrl.join(","));
        }
        router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    }

    function handleToggleLeague(leagueId: number) {
        const nextLeagueIds = selectedLeagueIds.includes(leagueId) ? selectedLeagueIds.filter((id) => id !== leagueId) : [...selectedLeagueIds, leagueId];
        syncFiltersInUrl(nextLeagueIds, selectedStatusId, selectedSortOrder);
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function handleSelectStatus(statusId: number) {
        syncFiltersInUrl(selectedLeagueIds, statusId);
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function handleSelectSort(sortOrder: MatchDateSortOrder) {
        syncFiltersInUrl(selectedLeagueIds, selectedStatusId, sortOrder);
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function handleSelectedSeasonYearsChange(years: string[]) {
        const hasLatestSeason = latestSeasonYear != null && years.includes(latestSeasonYear);
        const nextStatusId = hasLatestSeason ? selectedStatusId : ALL_STATUSES_ID;
        const nextSortOrder = hasLatestSeason ? selectedSortOrder : undefined;
        syncFiltersInUrl(selectedLeagueIds, nextStatusId, nextSortOrder, years);
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    const handleSearchQueryChange = useCallback(
        (value: string) => {
            const params = new URLSearchParams(searchParams.toString());
            const trimmed = value.trim();
            if (trimmed) {
                params.set("search", trimmed);
            } else {
                params.delete("search");
            }
            router.replace(`${pathname}?${params.toString()}`, { scroll: false });
        },
        [pathname, router, searchParams],
    );

    const filterPanelProps = {
        leagues,
        isLeaguesLoading,
        leaguesError,
        selectedLeagueIds,
        selectedStatusId,
        selectedSortOrder,
        searchQuery,
        seasonYears,
        isSeasonYearsLoading,
        seasonYearsError,
        selectedSeasonYears,
        onToggleLeague: handleToggleLeague,
        onSelectStatus: handleSelectStatus,
        onSelectSort: handleSelectSort,
        onSearchQueryChange: handleSearchQueryChange,
        onSelectedSeasonYearsChange: handleSelectedSeasonYearsChange,
    };

    const researchPanelProps = {
        summaryWidget,
        statsError,
        scopeLabel: researchStatsScopeLabel,
    };

    return (
        <main className="mx-auto w-full max-w-7xl px-4 pt-0 pb-8 sm:px-6 lg:py-8">
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,2.75fr)_minmax(0,1fr)] lg:items-start">
                <div
                    className={`sticky top-[var(--site-header-height)] z-40 order-1 -mx-4 flex flex-col gap-3 border-b border-zinc-200 bg-zinc-50 px-4 py-3 transition-transform duration-200 dark:border-zinc-800 dark:bg-zinc-950 sm:-mx-6 sm:px-6 motion-reduce:transition-none lg:hidden ${
                        isMobileChromeVisible ? "translate-y-0" : "-translate-y-full"
                    }`}
                >
                    <MatchFiltersMobileSheet {...filterPanelProps} sortParam={searchParams.get("sort")} latestSeasonYear={latestSeasonYear} />
                    <ResearchBettingMobileSheet {...researchPanelProps} />
                </div>
                <aside className="order-1 hidden min-h-0 flex-col gap-4 self-start lg:sticky lg:top-20 lg:flex lg:max-h-[calc(100dvh-5rem-var(--site-footer-height))] lg:overflow-y-auto lg:overscroll-contain">
                    <MatchFiltersPanel {...filterPanelProps} />
                </aside>
                <section className="order-3 min-w-0 lg:order-2">
                    {isLoading || !seasonFilterReady ? (
                        <MatchesFallback />
                    ) : error ? (
                        <p className="rounded-lg border border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/30 px-4 py-3 text-red-800 dark:text-red-200">{error}</p>
                    ) : (
                        <MatchList
                            matches={matches}
                            hasMore={hasMore}
                            isLoadingMore={isLoadingMore}
                            onLoadMore={handleLoadMoreMatches}
                            loadMoreError={loadMoreError}
                            onRetryLoadMore={handleRetryLoadMoreMatches}
                        />
                    )}
                </section>
                <aside className="order-2 hidden min-h-0 flex-col gap-3 self-start lg:order-3 lg:sticky lg:top-20 lg:flex lg:max-h-[calc(100dvh-5rem-var(--site-footer-height))] lg:overflow-y-auto lg:overscroll-contain">
                    <ResearchBettingPanel {...researchPanelProps} />
                </aside>
            </div>
        </main>
    );
}
