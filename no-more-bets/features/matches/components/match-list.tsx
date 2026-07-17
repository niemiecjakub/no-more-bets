"use client";

import Link from "next/link";
import { ChevronDown } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import type { MatchListItem } from "../interfaces";
import { MATCH_STATUS } from "../interfaces";
import { clubLogoSlugSegment } from "../../../utils/club-logo-slug";
import { formatMatchTime } from "../../../utils/format-date";
import { MatchListResearchPanel } from "./match-list-research-panel";

interface MatchListProps {
  matches: MatchListItem[];
  hasMore?: boolean;
  isLoadingMore?: boolean;
  onLoadMore?: () => void;
  loadMoreError?: string | null;
  onRetryLoadMore?: () => void;
}

interface MatchDateGroup {
  key: string;
  date: Date;
  matches: MatchListItem[];
}

function centerScoreOrTime(match: MatchListItem): string {
  if (match.matchStatusId === MATCH_STATUS.Finished && match.homeGoals != null && match.awayGoals != null) {
    return `${match.homeGoals} - ${match.awayGoals}`;
  }
  return formatMatchTime(match.matchDate);
}

function toDateKey(matchDate: string): string {
  const date = new Date(matchDate);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDateHeading(date: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "long",
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(date);
}

function getFutureDayDistanceLabel(date: Date): string | null {
  const now = new Date();
  const todayLocal = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  const dateLocal = new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
  const dayDiff = Math.floor((dateLocal - todayLocal) / (1000 * 60 * 60 * 24));

  if (dayDiff <= 0) return null;
  if (dayDiff === 1) return "in 1 day";
  return `in ${dayDiff} days`;
}

export function MatchList({
  matches,
  hasMore = false,
  isLoadingMore = false,
  onLoadMore,
  loadMoreError = null,
  onRetryLoadMore,
}: MatchListProps) {
  const sentinelRef = useRef<HTMLDivElement>(null);
  const [expandedMatchIds, setExpandedMatchIds] = useState<Set<number>>(() => new Set());

  const toggleExpanded = useCallback((matchId: number) => {
    setExpandedMatchIds((prev) => {
      const next = new Set(prev);
      if (next.has(matchId)) {
        next.delete(matchId);
      } else {
        next.add(matchId);
      }
      return next;
    });
  }, []);

  useEffect(() => {
    if (!hasMore || isLoadingMore || !onLoadMore) return;

    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          onLoadMore();
        }
      },
      { root: null, rootMargin: "200px", threshold: 0 },
    );

    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasMore, isLoadingMore, onLoadMore]);

  if (matches.length === 0) {
    return (
      <p className="text-center text-zinc-500 dark:text-zinc-400 py-8">
        No matches found.
      </p>
    );
  }

  const groups = matches.reduce<MatchDateGroup[]>((acc, match) => {
    const key = toDateKey(match.matchDate);
    const existing = acc.find((group) => group.key === key);
    if (existing) {
      existing.matches.push(match);
      return acc;
    }

    acc.push({
      key,
      date: new Date(match.matchDate),
      matches: [match],
    });
    return acc;
  }, []);

  return (
    <div className="flex flex-col gap-5">
      {groups.map((group) => {
        const futureLabel = getFutureDayDistanceLabel(group.date);
        return (
        <section key={group.key} className="flex flex-col gap-2">
          <h3 className="px-1 text-sm font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            {formatDateHeading(group.date)}
            {futureLabel ? (
              <span className="ml-2 text-xs text-zinc-500 dark:text-zinc-400 normal-case">
                ({futureLabel})
              </span>
            ) : null}
          </h3>
          <ul className="divide-y divide-zinc-200 dark:divide-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden">
            {group.matches.map((match) => {
              const center = centerScoreOrTime(match);
              const showScore =
                match.matchStatusId === MATCH_STATUS.Finished &&
                match.homeGoals != null &&
                match.awayGoals != null;
              const homeLogoSlug = clubLogoSlugSegment(
                match.homeClubSlug,
                match.homeClubName
              );
              const awayLogoSlug = clubLogoSlugSegment(
                match.awayClubSlug,
                match.awayClubName
              );
              const centerCell = showScore ? (
                <span className="inline-block text-center text-2xl font-bold tabular-nums tracking-tight text-foreground">
                  {center}
                </span>
              ) : (
                <time
                  dateTime={match.matchDate}
                  className="inline-block min-w-22 text-center text-2xl font-bold tabular-nums tracking-tight text-foreground"
                >
                  {center}
                </time>
              );
              const hasAgentResearch = match.hasResearch || match.hasResearchBet;
              const isExpanded = expandedMatchIds.has(match.id);
              const readinessChips = [
                { key: "lineup", label: "Lineup", isReady: match.hasLineup },
                { key: "odds", label: "Odds", isReady: match.hasOdds },
                { key: "h2h", label: "H2H", isReady: match.hasHeadToHead },
              ];

              return (
                <li key={match.id} className="bg-white dark:bg-zinc-950">
                  <div className="flex flex-col gap-1.5 px-4 py-3 transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900">
                    {match.leagueName || match.leagueSlug ? (
                      <Link
                        href={`/match/${match.id}`}
                        className="flex min-w-0 items-center justify-center gap-1.5"
                      >
                        <SlugIcon
                          kind="league"
                          slug={match.leagueSlug}
                          alt={match.leagueName || "League"}
                          className="h-4 w-4"
                        />
                        {match.leagueName ? (
                          <p className="min-w-0 truncate text-xs font-medium text-zinc-500 dark:text-zinc-400">
                            {match.leagueName}
                          </p>
                        ) : null}
                      </Link>
                    ) : null}
                    <div className="flex flex-col gap-1.5">
                      <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-4">
                        <Link
                          href={`/match/${match.id}`}
                          className="flex min-w-0 items-center justify-end gap-2"
                        >
                          <span className="min-w-0 truncate text-end font-medium text-foreground">
                            {match.homeClubName}
                          </span>
                          <SlugIcon
                            kind="club"
                            slug={homeLogoSlug}
                            alt={match.homeClubName}
                            className="h-7 w-7"
                          />
                        </Link>
                        <Link
                          href={`/match/${match.id}`}
                          className="justify-self-center px-3"
                        >
                          {centerCell}
                        </Link>
                        <Link
                          href={`/match/${match.id}`}
                          className="flex min-w-0 items-center justify-start gap-2"
                        >
                          <SlugIcon
                            kind="club"
                            slug={awayLogoSlug}
                            alt={match.awayClubName}
                            className="h-7 w-7"
                          />
                          <span className="min-w-0 truncate font-medium text-foreground">
                            {match.awayClubName}
                          </span>
                        </Link>
                      </div>
                      {hasAgentResearch ? (
                        <button
                          type="button"
                          aria-expanded={isExpanded}
                          aria-label={isExpanded ? "Collapse agent research" : "Expand agent research"}
                          onClick={() => toggleExpanded(match.id)}
                          className="mx-auto inline-flex items-center gap-1 rounded-full bg-sky-100 px-2.5 py-1 text-xs font-medium text-sky-900 ring-1 ring-inset ring-sky-600/25 transition-colors hover:bg-sky-200/80 dark:bg-sky-950/60 dark:text-sky-300 dark:ring-sky-500/35 dark:hover:bg-sky-900/70"
                        >
                          <span aria-hidden>🔬</span>
                          <span>Agent Research</span>
                          <ChevronDown
                            className={`h-3 w-3 transition-transform ${isExpanded ? "rotate-180" : ""}`}
                            aria-hidden
                          />
                        </button>
                      ) : (
                        <div className="flex flex-wrap items-center justify-center gap-1.5">
                          {readinessChips.map((chip) => (
                            <span
                              key={chip.key}
                              className={
                                chip.isReady
                                  ? "inline-flex items-center rounded-md bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30"
                                  : "inline-flex items-center rounded-md bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-700 ring-1 ring-inset ring-zinc-400/30 dark:bg-zinc-900/60 dark:text-zinc-300 dark:ring-zinc-600/40"
                              }
                            >
                              {chip.label}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                  {hasAgentResearch && isExpanded ? (
                    <MatchListResearchPanel matchId={match.id} />
                  ) : null}
                </li>
              );
            })}
          </ul>
        </section>
        );
      })}

      {loadMoreError ? (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 dark:border-red-900 dark:bg-red-950/30">
          <p className="text-sm text-red-800 dark:text-red-200">{loadMoreError}</p>
          {onRetryLoadMore ? (
            <button
              type="button"
              onClick={onRetryLoadMore}
              className="mt-2 text-sm font-medium text-red-900 underline-offset-2 hover:underline dark:text-red-100"
            >
              Retry
            </button>
          ) : null}
        </div>
      ) : null}

      {isLoadingMore ? (
        <div className="h-12 animate-pulse rounded-lg bg-zinc-100 dark:bg-zinc-900" />
      ) : null}

      {hasMore ? <div ref={sentinelRef} className="h-1" aria-hidden /> : null}
    </div>
  );
}
