"use client";

import axios from "axios";
import { notFound } from "next/navigation";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import { ClubBetSelectionChart } from "@/features/clubs/components/club-bet-selection-chart";
import { ClubLeagueTable } from "@/features/clubs/components/club-league-table";
import { ClubNextMatchCard } from "@/features/clubs/components/club-next-match-card";
import type { ClubBetSelectionStats, ClubDetail, ClubNextMatch } from "@/features/clubs/interfaces";
import {
  fetchClubBetSelectionStats,
  fetchClubById,
  fetchClubNextMatch,
  fetchClubRecentGames,
} from "@/features/clubs/services/club-detail-api";
import { fetchLeagueTable } from "@/features/leagues/services/leagues-api";
import type { LeagueTable } from "@/features/leagues/interfaces";
import { RecentGamesList } from "@/features/matches/components/recent-games-list";
import type { RecentMatch } from "@/features/matches/interfaces";
import { handleServiceError } from "@/lib/error-handler";

type SectionKey = "recentGames" | "leagueTable" | "nextMatch" | "betStats";

function initialSectionLoading(): Record<SectionKey, boolean> {
  return {
    recentGames: true,
    leagueTable: true,
    nextMatch: true,
    betStats: true,
  };
}

function SectionCard({
  title,
  icon,
  children,
  flush = false,
}: {
  title: string;
  icon: string;
  children: React.ReactNode;
  flush?: boolean;
}) {
  return (
    <section className="overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
        <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
          <span aria-hidden>{icon}</span>
          <span>{title}</span>
        </h2>
      </div>
      <div className={flush ? undefined : "px-4 py-4"}>{children}</div>
    </section>
  );
}

function NextMatchSkeleton() {
  return (
    <div className="flex flex-col gap-3 px-4 py-3">
      <div className="mx-auto h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-2">
        <div className="ml-auto h-6 w-20 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-8 w-16 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-6 w-20 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      <div className="mx-auto h-5 w-14 animate-pulse rounded-md bg-zinc-200 dark:bg-zinc-800" />
    </div>
  );
}

function ListSkeleton({ rows = 3 }: { rows?: number }) {
  return (
    <div className="space-y-2">
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className="h-14 animate-pulse rounded-md bg-zinc-200 dark:bg-zinc-800" />
      ))}
    </div>
  );
}

function TableSkeleton({ flush = false }: { flush?: boolean }) {
  return (
    <div className={flush ? "divide-y divide-zinc-200 dark:divide-zinc-800" : "space-y-2"}>
      {!flush ? <div className="h-4 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" /> : null}
      {Array.from({ length: 8 }, (_, i) => (
        <div
          key={i}
          className={`h-10 animate-pulse bg-zinc-200 dark:bg-zinc-800${flush ? "" : " rounded"}`}
        />
      ))}
    </div>
  );
}

function ChartSkeleton() {
  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:justify-center">
      <div className="h-[180px] w-[180px] animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
      <div className="space-y-2">
        <div className="h-4 w-36 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-4 w-36 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </div>
  );
}

function HeaderSkeleton() {
  return (
    <header className="mb-8 flex gap-3">
      <div className="h-16 w-16 shrink-0 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
      <div className="flex min-w-0 flex-col justify-center gap-2 py-0.5">
        <div className="h-8 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="flex items-center gap-2">
          <div className="h-5 w-5 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </header>
  );
}

function SectionError({ message }: { message: string }) {
  return <p className="text-sm text-red-800 dark:text-red-200">{message}</p>;
}

export default function ClubPage() {
  const params = useParams();
  const id = params?.id as string | undefined;
  const clubId = id != null && id !== "" ? Number(id) : NaN;
  const isValidId = !Number.isNaN(clubId) && clubId >= 1;

  const [club, setClub] = useState<ClubDetail | null>(null);
  const [clubLoading, setClubLoading] = useState(true);
  const [clubError, setClubError] = useState<string | null>(null);

  const [recentGames, setRecentGames] = useState<RecentMatch[] | undefined>();
  const [leagueTable, setLeagueTable] = useState<LeagueTable | undefined>();
  const [nextMatch, setNextMatch] = useState<ClubNextMatch | null | undefined>();
  const [betStats, setBetStats] = useState<ClubBetSelectionStats | undefined>();

  const [sectionLoading, setSectionLoading] = useState(initialSectionLoading);
  const [sectionErrors, setSectionErrors] = useState<Partial<Record<SectionKey, string>>>({});

  useEffect(() => {
    if (!isValidId) return;
    let isMounted = true;

    setClub(null);
    setClubLoading(true);
    setClubError(null);
    setRecentGames(undefined);
    setLeagueTable(undefined);
    setNextMatch(undefined);
    setBetStats(undefined);
    setSectionErrors({});
    setSectionLoading(initialSectionLoading());

    void fetchClubById(clubId)
      .then((data) => {
        if (!isMounted) return;
        setClub(data);
        setClubError(null);
      })
      .catch((err) => {
        if (!isMounted) return;
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setClub(null);
          setClubError("404");
          return;
        }
        setClubError(handleServiceError(err, "Failed to load club."));
      })
      .finally(() => {
        if (isMounted) setClubLoading(false);
      });

    const load = <K extends SectionKey>(key: K, fetcher: () => Promise<unknown>) => {
      void fetcher()
        .then((value) => {
          if (!isMounted) return;
          if (key === "recentGames") setRecentGames(value as RecentMatch[]);
          if (key === "leagueTable") setLeagueTable(value as LeagueTable);
          if (key === "nextMatch") setNextMatch(value as ClubNextMatch | null);
          if (key === "betStats") setBetStats(value as ClubBetSelectionStats);
          setSectionErrors((prev) => {
            if (!(key in prev)) return prev;
            const next = { ...prev };
            delete next[key];
            return next;
          });
        })
        .catch((err) => {
          if (!isMounted) return;
          if (key === "leagueTable" && axios.isAxiosError(err) && err.response?.status === 404) {
            setLeagueTable(undefined);
            return;
          }
          setSectionErrors((prev) => ({
            ...prev,
            [key]: handleServiceError(err, "Failed to load this section."),
          }));
        })
        .finally(() => {
          if (!isMounted) return;
          setSectionLoading((prev) => ({ ...prev, [key]: false }));
        });
    };

    load("recentGames", () => fetchClubRecentGames(clubId));
    load("nextMatch", () => fetchClubNextMatch(clubId));
    load("betStats", () => fetchClubBetSelectionStats(clubId));

    return () => {
      isMounted = false;
    };
  }, [clubId, isValidId]);

  useEffect(() => {
    if (!club) return;
    let isMounted = true;
    setSectionLoading((prev) => ({ ...prev, leagueTable: true }));
    setSectionErrors((prev) => {
      const next = { ...prev };
      delete next.leagueTable;
      return next;
    });

    void fetchLeagueTable(club.leagueId)
      .then((table) => {
        if (!isMounted) return;
        setLeagueTable(table);
      })
      .catch((err) => {
        if (!isMounted) return;
        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setLeagueTable(undefined);
          return;
        }
        setSectionErrors((prev) => ({
          ...prev,
          leagueTable: handleServiceError(err, "Failed to load league table."),
        }));
      })
      .finally(() => {
        if (isMounted) setSectionLoading((prev) => ({ ...prev, leagueTable: false }));
      });

    return () => {
      isMounted = false;
    };
  }, [club]);

  if (id != null && id !== "" && !isValidId) {
    notFound();
  }

  if (!clubLoading && clubError === "404") {
    notFound();
  }

  if (clubLoading && !club) {
    return (
      <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <HeaderSkeleton />
        <div className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2">
            <SectionCard title="Recent matches" icon="🕒">
              <ListSkeleton rows={5} />
            </SectionCard>
            <div className="flex flex-col gap-6">
              <SectionCard title="Next match" icon="📅" flush>
                <NextMatchSkeleton />
              </SectionCard>
              <SectionCard title="Research bet selections" icon="📊">
                <ChartSkeleton />
              </SectionCard>
            </div>
          </div>
          <SectionCard title="League table" icon="🏆" flush>
            <TableSkeleton flush />
          </SectionCard>
        </div>
      </main>
    );
  }

  if (clubError && !club) {
    return (
      <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
          {clubError}
        </p>
      </main>
    );
  }

  if (!club) {
    return null;
  }

  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <header className="mb-8 flex gap-3">
        <SlugIcon kind="club" slug={club.slug} alt={club.name} className="h-16 w-16 shrink-0" />
        <div className="flex min-w-0 flex-col justify-center gap-2 py-0.5">
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">{club.name}</h1>
          <p className="flex items-center gap-2 text-sm text-zinc-500 dark:text-zinc-400">
            <SlugIcon kind="league" slug={club.leagueSlug} alt={club.leagueName} className="h-5 w-5 shrink-0" />
            <span>{club.leagueName}</span>
          </p>
        </div>
      </header>

      <div className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2 md:items-start">
          <SectionCard title="Recent matches" icon="🕒">
            {sectionErrors.recentGames ? (
              <SectionError message={sectionErrors.recentGames} />
            ) : sectionLoading.recentGames && recentGames === undefined ? (
              <ListSkeleton rows={5} />
            ) : (
              <RecentGamesList games={recentGames} />
            )}
          </SectionCard>

          <div className="flex flex-col gap-6">
            <SectionCard title="Next match" icon="📅" flush>
              {sectionErrors.nextMatch ? (
                <div className="px-4 py-4">
                  <SectionError message={sectionErrors.nextMatch} />
                </div>
              ) : sectionLoading.nextMatch && nextMatch === undefined ? (
                <NextMatchSkeleton />
              ) : nextMatch ? (
                <ClubNextMatchCard
                  match={nextMatch}
                  leagueName={club.leagueName}
                  leagueSlug={club.leagueSlug}
                />
              ) : (
                <p className="px-4 py-4 text-sm text-zinc-500 dark:text-zinc-400">No upcoming match scheduled.</p>
              )}
            </SectionCard>

            <SectionCard title="Research bet selections" icon="📊">
              {sectionErrors.betStats ? (
                <SectionError message={sectionErrors.betStats} />
              ) : sectionLoading.betStats && betStats === undefined ? (
                <ChartSkeleton />
              ) : betStats ? (
                <ClubBetSelectionChart stats={betStats} />
              ) : (
                <p className="text-sm text-zinc-500 dark:text-zinc-400">No betting stats available.</p>
              )}
            </SectionCard>
          </div>
        </div>

        <SectionCard title="League table" icon="🏆" flush>
          {sectionErrors.leagueTable ? (
            <div className="px-4 py-4">
              <SectionError message={sectionErrors.leagueTable} />
            </div>
          ) : sectionLoading.leagueTable && leagueTable === undefined ? (
            <TableSkeleton flush />
          ) : leagueTable ? (
            <ClubLeagueTable table={leagueTable} highlightClubId={club.id} />
          ) : (
            <p className="px-4 py-4 text-sm text-zinc-500 dark:text-zinc-400">No league table data available.</p>
          )}
        </SectionCard>
      </div>
    </main>
  );
}
