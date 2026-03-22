"use client";

import { notFound } from "next/navigation";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { SlugIcon } from "@/components/slug-icon";
import { StructuredMatchAnalysisView } from "../../../features/matches/components/structured-match-analysis-view";
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
} from "@/features/matches/interfaces";
import {
  fetchMatchBettingOddsHistory,
  fetchMatchHeadToHead,
  fetchMatchInjuries,
  fetchMatchLeagueStatistics,
  fetchMatchLineups,
  fetchMatchPreview,
  fetchMatchRecentGames,
  fetchMatchRollingPerformance,
} from "@/features/matches/services/match-insights-api";

function LoadingSkeleton() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <div className="mb-1 h-7 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="space-y-4">
          {[1, 2].map((i) => (
            <div
              key={i}
              className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 overflow-hidden"
            >
              <div className="h-10 border-b border-zinc-200 dark:border-zinc-800 bg-zinc-100 dark:bg-zinc-900/50" />
              <div className="h-24 px-4 py-3" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}

export default function MatchPage() {
  const params = useParams();
  const id = params?.id as string | undefined;
  const matchId = id != null && id !== "" ? Number(id) : NaN;
  const isValidId = !Number.isNaN(matchId) && matchId >= 1;

  const {
    matchAnalysisById,
    isLoading,
    error,
    setMatchAnalysisPage,
  } = useMatchStore();

  const data = isValidId ? matchAnalysisById[matchId] : undefined;
  const [insights, setInsights] = useState<MatchInsights | null>(null);
  const [isInsightsLoading, setIsInsightsLoading] = useState(false);
  const [insightsError, setInsightsError] = useState<string | null>(null);

  useEffect(() => {
    if (!isValidId) return;
    setMatchAnalysisPage(matchId);
  }, [matchId, isValidId, setMatchAnalysisPage]);

  useEffect(() => {
    if (!isValidId) return;
    let isMounted = true;

    const loadInsights = async () => {
      setIsInsightsLoading(true);
      setInsightsError(null);
      try {
        const [
          lineups,
          injuries,
          preview,
          recentGames,
          leagueStatistics,
          headToHead,
          bettingOddsHistory,
          rollingPerformance,
        ] = await Promise.all([
          fetchMatchLineups(matchId),
          fetchMatchInjuries(matchId),
          fetchMatchPreview(matchId),
          fetchMatchRecentGames(matchId),
          fetchMatchLeagueStatistics(matchId),
          fetchMatchHeadToHead(matchId),
          fetchMatchBettingOddsHistory(matchId),
          fetchMatchRollingPerformance(matchId),
        ]);

        if (!isMounted) return;
        setInsights({
          lineups,
          injuries,
          preview,
          recentGames,
          leagueStatistics,
          headToHead,
          bettingOddsHistory,
          rollingPerformance,
        });
      } catch (err) {
        if (!isMounted) return;
        setInsightsError(handleServiceError(err, "Failed to load match sections."));
      } finally {
        if (isMounted) setIsInsightsLoading(false);
      }
    };

    loadInsights();

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
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
        <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        </main>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  const matchDateFormatted = formatMatchDate(data.matchDate);
  const homeLogoSlug = clubLogoSlugSegment(data.homeClubSlug, data.homeClubName);
  const awayLogoSlug = clubLogoSlugSegment(data.awayClubSlug, data.awayClubName);
  const showFinishedScore =
    data.matchStatusId === MATCH_STATUS.Finished &&
    data.homeGoals != null &&
    data.awayGoals != null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <header className="mb-6 flex flex-col items-center">
          <h1 className="grid w-full max-w-3xl grid-cols-1 items-center gap-3 text-2xl font-semibold tracking-tight text-foreground sm:grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] sm:gap-x-3 sm:gap-y-0">
            <span className="flex min-w-0 items-center justify-center gap-2.5 sm:justify-end">
              <span className="min-w-0 text-balance text-end">{data.homeClubName}</span>
              <SlugIcon kind="club" slug={homeLogoSlug} alt={data.homeClubName} className="h-10 w-10" />
            </span>
            <span
              className={
                showFinishedScore
                  ? "inline-block min-w-[5.5rem] shrink-0 text-center text-2xl font-bold tabular-nums tracking-tight text-foreground sm:text-3xl"
                  : "shrink-0 text-center text-lg font-medium text-zinc-500 dark:text-zinc-400 sm:text-2xl sm:font-semibold"
              }
            >
              {showFinishedScore ? `${data.homeGoals} - ${data.awayGoals}` : "vs"}
            </span>
            <span className="flex min-w-0 items-center justify-center gap-2.5 sm:justify-start">
              <SlugIcon kind="club" slug={awayLogoSlug} alt={data.awayClubName} className="h-10 w-10" />
              <span className="min-w-0 text-balance text-start">{data.awayClubName}</span>
            </span>
          </h1>
          <p className="mt-2 text-center text-sm text-zinc-500 dark:text-zinc-400">{matchDateFormatted}</p>
        </header>

        {insightsError ? (
          <p className="mb-6 rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-amber-900 dark:border-amber-800 dark:bg-amber-950/30 dark:text-amber-200">
            {insightsError}
          </p>
        ) : null}

        <section className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
          <div className="space-y-6">
            <Card title="Lineups" icon="📋">
              {isInsightsLoading && !insights ? (
                <div className="px-4 py-4">
                  <MutedText>Loading lineups...</MutedText>
                </div>
              ) : (
                <TeamColumns
                  homeClubName={data.homeClubName}
                  awayClubName={data.awayClubName}
                  homeLogoSlug={homeLogoSlug}
                  awayLogoSlug={awayLogoSlug}
                  home={<LineupList lineup={insights?.lineups?.home} />}
                  away={<LineupList lineup={insights?.lineups?.away} />}
                />
              )}
            </Card>

            <Card title="Injuries / Unavailable players" icon="🏥">
              {isInsightsLoading && !insights ? (
                <div className="px-4 py-4">
                  <MutedText>Loading injuries...</MutedText>
                </div>
              ) : (
                <TeamColumns
                  homeClubName={data.homeClubName}
                  awayClubName={data.awayClubName}
                  homeLogoSlug={homeLogoSlug}
                  awayLogoSlug={awayLogoSlug}
                  home={<InjuriesList injuries={insights?.injuries?.home} />}
                  away={<InjuriesList injuries={insights?.injuries?.away} />}
                />
              )}
            </Card>

            <Card title="Recent games per club" icon="🕒">
              {isInsightsLoading && !insights ? (
                <div className="px-4 py-4">
                  <MutedText>Loading recent games...</MutedText>
                </div>
              ) : (
                <TeamColumns
                  homeClubName={data.homeClubName}
                  awayClubName={data.awayClubName}
                  homeLogoSlug={homeLogoSlug}
                  awayLogoSlug={awayLogoSlug}
                  home={<RecentGamesList games={insights?.recentGames?.home} />}
                  away={<RecentGamesList games={insights?.recentGames?.away} />}
                />
              )}
            </Card>

            <Card title="Rolling performance" icon="📈">
              {isInsightsLoading && !insights ? (
                <div className="px-4 py-4">
                  <MutedText>Loading rolling performance...</MutedText>
                </div>
              ) : (
                <TeamColumns
                  homeClubName={data.homeClubName}
                  awayClubName={data.awayClubName}
                  homeLogoSlug={homeLogoSlug}
                  awayLogoSlug={awayLogoSlug}
                  home={<RollingPerformanceSection data={insights?.rollingPerformance?.home} />}
                  away={<RollingPerformanceSection data={insights?.rollingPerformance?.away} />}
                />
              )}
            </Card>

            <Card title="Match analysis" icon="🧠">
              {data.analyses.length === 0 ? (
                <div className="px-4 py-4">
                  <MutedText>Analysis not available yet.</MutedText>
                </div>
              ) : (
                <ul className="flex flex-col gap-6 px-4 py-4">
                  {data.analyses.map((analysis) => (
                    <li
                      key={analysis.id}
                      className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
                    >
                      <h3 className="border-b border-zinc-200 px-4 py-3 text-base font-semibold text-foreground dark:border-zinc-800">
                        {analysis.code}
                      </h3>
                      <div className="px-4 pb-4 pt-2">
                        {analysis.structured ? (
                          <StructuredMatchAnalysisView analysis={analysis.structured} />
                        ) : (
                          <MutedText>Analysis not available.</MutedText>
                        )}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
          </div>

          <div className="space-y-6">
            <Card title="Match preview" icon="📰">
              <div className="px-4 py-4">
                <PreviewSection preview={insights?.preview} isLoading={isInsightsLoading && !insights} />
              </div>
            </Card>

            <Card title="League statistics" icon="🏆">
              {isInsightsLoading && !insights ? (
                <div className="px-4 py-4">
                  <MutedText>Loading league statistics...</MutedText>
                </div>
              ) : (
                <TeamColumns
                  homeClubName={data.homeClubName}
                  awayClubName={data.awayClubName}
                  homeLogoSlug={homeLogoSlug}
                  awayLogoSlug={awayLogoSlug}
                  home={<LeagueStatsSection stats={insights?.leagueStatistics?.home} />}
                  away={<LeagueStatsSection stats={insights?.leagueStatistics?.away} />}
                />
              )}
            </Card>

            <Card title="Head-to-head stats" icon="⚔️">
              <HeadToHeadSection
                data={insights?.headToHead}
                isLoading={isInsightsLoading && !insights}
                homeLogoSlug={homeLogoSlug}
                awayLogoSlug={awayLogoSlug}
              />
            </Card>

            <Card title="Betting odds movement / history" icon="💹">
              <BettingOddsSection data={insights?.bettingOddsHistory} isLoading={isInsightsLoading && !insights} />
            </Card>
          </div>
        </section>
      </main>
    </div>
  );
}

interface MatchInsights {
  lineups: MatchLineupResult | null;
  injuries: MatchInjuriesResult | null;
  preview: string | null;
  recentGames: ClubPair<RecentMatch[] | null>;
  leagueStatistics: ClubPair<ClubLeagueStats | null>;
  headToHead: HeadToHead | null;
  bettingOddsHistory: MarketPriceHistory[] | null;
  rollingPerformance: ClubPair<TeamPerformanceResult | null>;
}

interface CardProps {
  title: string;
  icon: string;
  children: React.ReactNode;
}

function Card({ title, icon, children }: CardProps) {
  return (
    <section className="overflow-hidden rounded-xl border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <details open className="group">
        <summary className="flex cursor-pointer list-none items-center justify-between border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
          <h2 className="flex items-center gap-2 text-base font-semibold text-foreground">
            <span aria-hidden>{icon}</span>
            <span>{title}</span>
          </h2>
          <span className="text-sm text-zinc-500 transition-transform group-open:rotate-180 dark:text-zinc-400">
            ▼
          </span>
        </summary>
        <div>{children}</div>
      </details>
    </section>
  );
}

function MutedText({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-zinc-500 dark:text-zinc-400">{children}</p>;
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

function RecentGamesList({ games }: { games?: RecentMatch[] | null }) {
  if (!games || games.length === 0) return <MutedText>No recent games available.</MutedText>;
  return (
    <ul className="flex flex-col gap-2 text-sm">
      {games.map((game) => (
        <li key={game.matchId} className="flex items-center justify-between gap-2 rounded-md border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-950">
          <div className="flex min-w-0 items-center gap-2.5">
            <SlugIcon
              kind="club"
              slug={clubLogoSlugSegment(null, game.opponent)}
              alt={game.opponent}
              className="h-7 w-7"
            />
            <div className="min-w-0">
              <p className="truncate font-medium text-foreground">{game.opponent}</p>
              <p className="text-xs text-zinc-500 dark:text-zinc-400">{game.date}</p>
            </div>
          </div>
          <div className="flex shrink-0 items-center justify-end gap-2">
            <p className="font-semibold tabular-nums text-foreground">{game.score}</p>
            <ResultBadge result={game.result} />
          </div>
        </li>
      ))}
    </ul>
  );
}

function ResultBadge({ result }: { result: string }) {
  const className =
    result === "Win"
      ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300"
      : result === "Loss"
        ? "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
        : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300";
  return <span className={`inline-flex rounded px-2 py-0.5 text-xs font-medium ${className}`}>{result}</span>;
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

function HeadToHeadSection({
  data,
  isLoading,
  homeLogoSlug,
  awayLogoSlug,
}: {
  data?: HeadToHead | null;
  isLoading: boolean;
  homeLogoSlug: string;
  awayLogoSlug: string;
}) {
  if (isLoading && !data) {
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

function BettingOddsSection({ data, isLoading }: { data?: MarketPriceHistory[] | null; isLoading: boolean }) {
  if (isLoading && !data) {
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
          <h4 className="mb-3 text-sm font-semibold text-foreground">
            {market.marketDisplayName ?? market.marketKey}
          </h4>
          <ul className="flex flex-col gap-2 text-sm">
            {market.outcomes.map((outcome) => (
              <li key={outcome.outcomeName} className="flex justify-between gap-3">
                <span className="text-zinc-600 dark:text-zinc-300">{outcome.outcomeName}</span>
                <span className="shrink-0 font-medium tabular-nums text-foreground">
                  {outcome.timeline.length === 0
                    ? "No data"
                    : `${outcome.timeline[0].price.toFixed(2)} -> ${outcome.timeline[outcome.timeline.length - 1].price.toFixed(2)}`}
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
    </div>
  );
}

function PreviewSection({ preview, isLoading }: { preview?: string | null; isLoading: boolean }) {
  if (isLoading && preview == null) return <MutedText>Loading preview...</MutedText>;
  if (!preview) return <MutedText>No preview available.</MutedText>;
  return <p className="whitespace-pre-wrap text-sm leading-6 text-zinc-700 dark:text-zinc-300">{preview}</p>;
}
