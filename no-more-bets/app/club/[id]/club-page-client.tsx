"use client";

import axios from "axios";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { ChevronDown } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Breadcrumbs } from "@/components/breadcrumbs";
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
  fetchClubMatches,
} from "@/features/clubs/services/club-detail-api";
import type { MatchListItem, RecentMatch } from "@/features/matches/interfaces";
import { fetchLeagueTable } from "@/features/leagues/services/leagues-api";
import type { LeagueTable } from "@/features/leagues/interfaces";
import { handleServiceError } from "@/lib/error-handler";
import { resolveDefaultMembership } from "@/features/clubs/resolve-default-membership";
import { cn } from "@/lib/utils";

type SectionKey = "recentGames" | "leagueTable" | "nextMatch" | "betStats" | "clubMatches";
type ClubTab = "general" | "matches";

function resolveRequestedTab(searchParams: URLSearchParams): ClubTab | null {
  const requestedTab = searchParams.get("tab");
  if (requestedTab === "general" || requestedTab === "matches") return requestedTab;
  return null;
}

function initialSectionLoading(hasTable: boolean): Record<SectionKey, boolean> {
  return {
    recentGames: false,
    leagueTable: !hasTable,
    nextMatch: false,
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
  "mb-6 flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white px-4 py-4 dark:border-zinc-800 dark:bg-zinc-950 sm:flex-row sm:items-center sm:gap-5 sm:px-5";

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
      className="flex w-full shrink-0 items-center gap-2 sm:ml-auto sm:w-auto sm:gap-2.5"
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
            "min-w-22 flex-1 rounded-lg border px-4 py-2 text-sm font-semibold tracking-tight transition-all sm:flex-none",
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

function ClubSeasonSelect({
  memberships,
  selected,
  onChange,
}: {
  memberships: ClubSeasonMembership[];
  selected: ClubSeasonMembership;
  onChange: (seasonId: number) => void;
}) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={rootRef} className="relative inline-flex max-w-full">
      <label id="club-season-label" className="sr-only">Season</label>
      <button
        type="button"
        id="club-season"
        aria-labelledby="club-season-label"
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
        className="inline-flex max-w-full items-center gap-2 rounded-md border border-zinc-200 bg-white py-1 pr-2 pl-2 text-sm text-foreground dark:border-zinc-700 dark:bg-zinc-900"
      >
        <SlugIcon
          kind="league"
          slug={selected.leagueSlug}
          alt={selected.leagueName}
          className="h-5 w-5 shrink-0"
        />
        <span className="truncate">
          {selected.leagueName} {selected.seasonYear}
        </span>
        <ChevronDown className="size-4 shrink-0 text-zinc-500 dark:text-zinc-400" aria-hidden />
      </button>
      {open ? (
        <ul
          role="listbox"
          aria-labelledby="club-season-label"
          className="absolute top-full left-0 z-20 mt-1 min-w-full overflow-hidden rounded-md border border-zinc-200 bg-white py-1 shadow-md dark:border-zinc-700 dark:bg-zinc-900"
        >
          {memberships.map((membership) => {
            const isSelected = membership.seasonId === selected.seasonId;
            return (
              <li key={membership.seasonId} role="presentation">
                <button
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  onClick={() => {
                    onChange(membership.seasonId);
                    setOpen(false);
                  }}
                  className={cn(
                    "flex w-full items-center gap-2 px-2 py-1.5 text-left text-sm text-foreground",
                    isSelected
                      ? "bg-zinc-100 dark:bg-zinc-800"
                      : "hover:bg-zinc-50 dark:hover:bg-zinc-800/70",
                  )}
                >
                  <SlugIcon
                    kind="league"
                    slug={membership.leagueSlug}
                    alt={membership.leagueName}
                    className="h-5 w-5 shrink-0"
                  />
                  <span className="truncate">
                    {membership.leagueName} {membership.seasonYear}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}

function SectionError({ message }: { message: string }) {
  return <p className="text-sm text-red-800 dark:text-red-200">{message}</p>;
}

export function ClubPageClient({
  initialClub,
  initialNextMatch,
  initialRecentGames,
  initialTable,
}: {
  initialClub: ClubDetail;
  initialNextMatch: ClubNextMatch | null;
  initialRecentGames: RecentMatch[];
  initialTable: LeagueTable | null;
}) {
  const club = initialClub;
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const recentGames = initialRecentGames;
  const [clubMatches, setClubMatches] = useState<MatchListItem[] | undefined>();
  const [leagueTable, setLeagueTable] = useState<LeagueTable | undefined>(initialTable ?? undefined);
  const nextMatch = initialNextMatch;
  const [betStats, setBetStats] = useState<ClubBetSelectionStats | undefined>();
  const requestedSeasonId = Number(searchParams.get("seasonId"));
  const memberships = club.memberships;
  const selectedMembership = memberships.find(
    (membership) => membership.seasonId === requestedSeasonId,
  ) ?? resolveDefaultMembership(club.memberships);
  const displayedLeagueTable = leagueTable?.seasonId === selectedMembership?.seasonId
    ? leagueTable
    : undefined;

  const [activeTab, setActiveTab] = useState<ClubTab>(() => {
    const requestedTab = resolveRequestedTab(new URLSearchParams(searchParams.toString()));
    return requestedTab ?? "general";
  });
  const [sectionLoading, setSectionLoading] = useState(() =>
    initialSectionLoading(initialTable != null),
  );
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
    let isMounted = true;
    setClubMatches(undefined);
    setBetStats(undefined);
    setSectionLoading((prev) => ({
      ...prev,
      clubMatches: true,
      betStats: true,
      recentGames: false,
      nextMatch: false,
      leagueTable: initialTable == null,
    }));

    const load = <K extends SectionKey>(key: K, fetcher: () => Promise<unknown>) => {
      void fetcher()
        .then((value) => {
          if (!isMounted) return;
          if (key === "clubMatches") setClubMatches(value as MatchListItem[]);
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

    load("clubMatches", () => fetchClubMatches(club.id));
    load("betStats", () => fetchClubBetSelectionStats(club.id));

    return () => {
      isMounted = false;
    };
  }, [club.id, initialTable]);

  useEffect(() => {
    if (!selectedMembership) return;
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
  }, [club.id, selectedMembership]);

  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <Breadcrumbs
        items={[
          { name: "Home", href: "/" },
          { name: club.name },
        ]}
      />
      <header className={clubHeaderClassName}>
        <div className="flex min-w-0 flex-1 items-center gap-4 sm:gap-5">
          <SlugIcon kind="club" slug={club.slug} alt={club.name} size={64} className="h-16 w-16 shrink-0" />
          <div className="flex min-w-0 flex-1 flex-col justify-center gap-2 py-0.5">
            <h1 className="text-2xl font-semibold tracking-tight text-foreground">{club.name}</h1>
            {selectedMembership ? (
              <ClubSeasonSelect
                memberships={memberships}
                selected={selectedMembership}
                onChange={handleSeasonChange}
              />
            ) : (
              <p className="text-sm text-zinc-500 dark:text-zinc-400">No season memberships</p>
            )}
          </div>
        </div>
        <ClubHeaderTabs active={activeTab} onChange={handleTabChange} />
      </header>

      <div className="space-y-6">
        {activeTab === "general" ? (
          <>
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
