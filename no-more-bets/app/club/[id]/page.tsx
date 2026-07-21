"use client";

import axios from "axios";
import { notFound } from "next/navigation";
import { useParams, usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import { ClubBetSelectionChart } from "@/features/clubs/components/club-bet-selection-chart";
import { ClubLeagueTable } from "@/features/clubs/components/club-league-table";
import { ClubWorldCupGroupTables } from "@/features/clubs/components/club-world-cup-group-tables";
import { ClubNextMatchCard } from "@/features/clubs/components/club-next-match-card";
import { ClubRecentMatchesPanel } from "@/features/clubs/components/club-recent-matches-panel";
import { MatchList } from "@/features/matches/components/match-list";
import type {
  ClubBetSelectionStats,
  ClubDetail,
  ClubNextMatch,
  ClubSeasonMembership,
} from "@/features/clubs/interfaces";
import {
  fetchClubBetSelectionStats,
  fetchClubById,
  fetchClubMatches,
  fetchClubNextMatch,
  fetchClubRecentGames,
} from "@/features/clubs/services/club-detail-api";
import type { MatchListItem } from "@/features/matches/interfaces";
import { fetchLeagueTable } from "@/features/leagues/services/leagues-api";
import type { LeagueTable } from "@/features/leagues/interfaces";
import type { RecentMatch } from "@/features/matches/interfaces";
import { handleServiceError } from "@/lib/error-handler";
import { cn } from "@/lib/utils";

type SectionKey = "recentGames" | "leagueTable" | "nextMatch" | "betStats" | "clubMatches";
type ClubTab = "general" | "matches";

function resolveRequestedTab(searchParams: URLSearchParams): ClubTab | null {
  const requestedTab = searchParams.get("tab");
  if (requestedTab === "general" || requestedTab === "matches") return requestedTab;
  return null;
}

function resolveDefaultMembership(memberships: ClubSeasonMembership[]): ClubSeasonMembership | null {
  const today = new Date().toISOString().slice(0, 10);
  return memberships.find((membership) => !membership.startDate || membership.startDate <= today)
    ?? memberships[0]
    ?? null;
}

function initialSectionLoading(): Record<SectionKey, boolean> {
  return {
    recentGames: true,
    leagueTable: true,
    nextMatch: true,
    betStats: true,
    clubMatches: true,
  };
}

function LeagueTableHeader({ leagueName, leagueSlug }: { leagueName: string; leagueSlug: string }) {
  return (
    <>
      <SlugIcon kind="league" slug={leagueSlug} alt={leagueName} className="h-5 w-5 shrink-0" />
      <span>{leagueName}</span>
    </>
  );
}

function SectionCard({
  title,
  icon,
  children,
  flush = false,
}: {
  title: React.ReactNode;
  icon?: string;
  children: React.ReactNode;
  flush?: boolean;
}) {
  return (
    <section className="overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
        <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
          {icon ? <span aria-hidden>{icon}</span> : null}
          {typeof title === "string" ? <span>{title}</span> : title}
        </h2>
      </div>
      <div className={flush ? undefined : "px-4 py-4"}>{children}</div>
    </section>
  );
}

function ClubMatchesSkeleton() {
  return (
    <div className="animate-pulse space-y-5">
      {[1, 2].map((group) => (
        <section key={group} className="space-y-2">
          <div className="h-4 w-56 rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
            {[1, 2, 3].map((row) => (
              <div
                key={`${group}-${row}`}
                className="space-y-2 border-b border-zinc-200 bg-white px-4 py-3 last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950"
              >
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
              </div>
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

function RecentMatchesSkeleton() {
  return (
    <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
      <div className="space-y-2">
        {Array.from({ length: 5 }, (_, i) => (
          <div key={i} className="h-14 animate-pulse rounded-md border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950" />
        ))}
      </div>
    </div>
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

const clubHeaderClassName =
  "mb-6 flex items-center gap-4 rounded-xl border border-zinc-200 bg-white px-4 py-4 dark:border-zinc-800 dark:bg-zinc-950 sm:gap-5 sm:px-5";

function ClubHeaderTabs({
  active,
  onChange,
}: {
  active: ClubTab;
  onChange: (tab: ClubTab) => void;
}) {
  const tabs: { id: ClubTab; label: string }[] = [
    { id: "general", label: "General" },
    { id: "matches", label: "Matches" },
  ];

  return (
    <div
      className="ml-auto flex shrink-0 items-center gap-2 self-center sm:gap-2.5"
      role="tablist"
      aria-label="Club view"
    >
      {tabs.map(({ id, label }) => (
        <button
          key={id}
          type="button"
          role="tab"
          aria-selected={active === id}
          onClick={() => onChange(id)}
          className={cn(
            "min-w-22 rounded-lg border px-4 py-2 text-sm font-semibold tracking-tight transition-all",
            active === id
              ? "border-zinc-300 bg-white text-foreground shadow-sm dark:border-zinc-600 dark:bg-zinc-800 dark:shadow-none"
              : "border-zinc-200/80 bg-zinc-50 text-zinc-600 hover:border-zinc-300 hover:bg-white hover:text-foreground dark:border-zinc-800 dark:bg-zinc-900/60 dark:text-zinc-400 dark:hover:border-zinc-700 dark:hover:bg-zinc-900 dark:hover:text-zinc-200",
          )}
        >
          {label}
        </button>
      ))}
    </div>
  );
}

function HeaderSkeleton() {
  return (
    <header className={clubHeaderClassName}>
      <div className="h-16 w-16 shrink-0 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
      <div className="flex min-w-0 flex-1 flex-col justify-center gap-2 py-0.5">
        <div className="h-8 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="flex items-center gap-2">
          <div className="h-5 w-5 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
      <div className="ml-auto flex shrink-0 gap-2 self-center sm:gap-2.5">
        <div className="h-9 w-24 animate-pulse rounded-lg bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-9 w-24 animate-pulse rounded-lg bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </header>
  );
}

function SectionError({ message }: { message: string }) {
  return <p className="text-sm text-red-800 dark:text-red-200">{message}</p>;
}

export default function ClubPage() {
  const params = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const id = params?.id as string | undefined;
  const clubId = id != null && id !== "" ? Number(id) : NaN;
  const isValidId = !Number.isNaN(clubId) && clubId >= 1;

  const [club, setClub] = useState<ClubDetail | null>(null);
  const [clubLoading, setClubLoading] = useState(true);
  const [clubError, setClubError] = useState<string | null>(null);

  const [recentGames, setRecentGames] = useState<RecentMatch[] | undefined>();
  const [clubMatches, setClubMatches] = useState<MatchListItem[] | undefined>();
  const [leagueTable, setLeagueTable] = useState<LeagueTable | undefined>();
  const [nextMatch, setNextMatch] = useState<ClubNextMatch | null | undefined>();
  const [betStats, setBetStats] = useState<ClubBetSelectionStats | undefined>();
  const requestedSeasonId = Number(searchParams.get("seasonId"));
  const memberships = club?.memberships ?? [];
  const selectedMembership = memberships.find(
    (membership) => membership.seasonId === requestedSeasonId,
  ) ?? (club ? resolveDefaultMembership(club.memberships) : null);
  const displayedLeagueTable = leagueTable?.seasonId === selectedMembership?.seasonId
    ? leagueTable
    : undefined;

  const [activeTab, setActiveTab] = useState<ClubTab>(() => {
    const requestedTab = resolveRequestedTab(new URLSearchParams(searchParams.toString()));
    return requestedTab ?? "general";
  });
  const [sectionLoading, setSectionLoading] = useState(initialSectionLoading);
  const [sectionErrors, setSectionErrors] = useState<Partial<Record<SectionKey, string>>>({});

  useEffect(() => {
    const requestedTab = resolveRequestedTab(new URLSearchParams(searchParams.toString()));
    setActiveTab(requestedTab ?? "general");
  }, [searchParams]);

  const handleTabChange = useCallback(
    (tab: ClubTab) => {
      setActiveTab(tab);
      const params = new URLSearchParams(searchParams.toString());
      if (tab === "general") {
        params.delete("tab");
      } else {
        params.set("tab", tab);
      }
      const query = params.toString();
      router.push(query ? `${pathname}?${query}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  const handleSeasonChange = useCallback(
    (seasonId: number) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("seasonId", seasonId.toString());
      router.push(`${pathname}?${params.toString()}`, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  useEffect(() => {
    if (!selectedMembership || searchParams.get("seasonId") === selectedMembership.seasonId.toString()) return;
    const params = new URLSearchParams(searchParams.toString());
    params.set("seasonId", selectedMembership.seasonId.toString());
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams, selectedMembership]);

  useEffect(() => {
    if (!isValidId) return;
    let isMounted = true;

    setClub(null);
    setClubLoading(true);
    setClubError(null);
    setRecentGames(undefined);
    setClubMatches(undefined);
    setLeagueTable(undefined);
    setNextMatch(undefined);
    setBetStats(undefined);
    setSectionErrors({});
    setSectionLoading(initialSectionLoading());

    void fetchClubById(clubId)
      .then((data) => {
        if (!isMounted) return;
        setClub(data);
        if (data.memberships.length === 0) {
          setSectionLoading((prev) => ({ ...prev, leagueTable: false }));
        }
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
          if (key === "clubMatches") setClubMatches(value as MatchListItem[]);
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
    load("clubMatches", () => fetchClubMatches(clubId));
    load("nextMatch", () => fetchClubNextMatch(clubId));
    load("betStats", () => fetchClubBetSelectionStats(clubId));

    return () => {
      isMounted = false;
    };
  }, [clubId, isValidId]);

  useEffect(() => {
    if (!club || !selectedMembership) return;
    let isMounted = true;
    setSectionLoading((prev) => ({ ...prev, leagueTable: true }));
    setSectionErrors((prev) => {
      const next = { ...prev };
      delete next.leagueTable;
      return next;
    });

    void fetchLeagueTable(
      selectedMembership.leagueId,
      selectedMembership.seasonId,
      selectedMembership.leagueSlug === "fifa-world-cup" ? club.id : undefined,
    )
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
  }, [club, selectedMembership]);

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
            <SectionCard title="Recent matches" icon="🕒" flush>
              <RecentMatchesSkeleton />
            </SectionCard>
            <div className="flex flex-col gap-6">
              <SectionCard title="Next match" icon="📅" flush>
                <NextMatchSkeleton />
              </SectionCard>
              <SectionCard title="Research bet stats" icon="📊">
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
      <header className={clubHeaderClassName}>
        <SlugIcon kind="club" slug={club.slug} alt={club.name} className="h-16 w-16 shrink-0" />
        <div className="flex min-w-0 flex-1 flex-col justify-center gap-2 py-0.5">
          <h1 className="text-2xl font-semibold tracking-tight text-foreground">{club.name}</h1>
          {selectedMembership ? (
            <div className="flex items-center gap-2 text-sm text-zinc-500 dark:text-zinc-400">
              <SlugIcon
                kind="league"
                slug={selectedMembership.leagueSlug}
                alt={selectedMembership.leagueName}
                className="h-5 w-5 shrink-0"
              />
              <label htmlFor="club-season" className="sr-only">Season</label>
              <select
                id="club-season"
                value={selectedMembership.seasonId}
                onChange={(event) => handleSeasonChange(Number(event.target.value))}
                className="max-w-full rounded-md border border-zinc-200 bg-white px-2 py-1 text-sm text-foreground dark:border-zinc-700 dark:bg-zinc-900"
              >
                {memberships.map((membership) => (
                  <option key={membership.seasonId} value={membership.seasonId}>
                    {membership.leagueName} {membership.seasonYear}
                  </option>
                ))}
              </select>
            </div>
          ) : (
            <p className="text-sm text-zinc-500 dark:text-zinc-400">No season memberships</p>
          )}
        </div>
        <ClubHeaderTabs active={activeTab} onChange={handleTabChange} />
      </header>

      <div className="space-y-6">
        {activeTab === "general" ? (
          <>
            <div className="grid gap-6 md:grid-cols-2 md:items-start">
              <SectionCard title="Recent matches" icon="🕒" flush>
                {sectionErrors.recentGames ? (
                  <div className="px-4 py-4">
                    <SectionError message={sectionErrors.recentGames} />
                  </div>
                ) : sectionLoading.recentGames && recentGames === undefined ? (
                  <RecentMatchesSkeleton />
                ) : (
                  <ClubRecentMatchesPanel games={recentGames} />
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
                      leagueName={selectedMembership?.leagueName ?? ""}
                      leagueSlug={selectedMembership?.leagueSlug ?? ""}
                    />
                  ) : (
                    <p className="px-4 py-4 text-sm text-zinc-500 dark:text-zinc-400">No upcoming match scheduled.</p>
                  )}
                </SectionCard>

                <SectionCard title="Research bet stats" icon="📊">
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

            <SectionCard
              title={selectedMembership
                ? <LeagueTableHeader
                    leagueName={`${selectedMembership.leagueName} ${selectedMembership.seasonYear}`}
                    leagueSlug={selectedMembership.leagueSlug}
                  />
                : "League table"}
              flush
            >
              {sectionErrors.leagueTable ? (
                <div className="px-4 py-4">
                  <SectionError message={sectionErrors.leagueTable} />
                </div>
              ) : sectionLoading.leagueTable && displayedLeagueTable === undefined ? (
                <TableSkeleton flush />
              ) : displayedLeagueTable ? (
                displayedLeagueTable.groups && displayedLeagueTable.groups.length > 0 ? (
                  <ClubWorldCupGroupTables table={displayedLeagueTable} highlightClubId={club.id} />
                ) : (
                  <ClubLeagueTable table={displayedLeagueTable} highlightClubId={club.id} />
                )
              ) : (
                <p className="px-4 py-4 text-sm text-zinc-500 dark:text-zinc-400">No league table data available.</p>
              )}
            </SectionCard>
          </>
        ) : sectionErrors.clubMatches ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {sectionErrors.clubMatches}
          </p>
        ) : sectionLoading.clubMatches && clubMatches === undefined ? (
          <ClubMatchesSkeleton />
        ) : (
          <MatchList matches={clubMatches ?? []} />
        )}
      </div>
    </main>
  );
}
