import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import type { MatchListItem } from "../interfaces";
import { MATCH_STATUS } from "../interfaces";
import { clubLogoSlugSegment } from "../../../utils/club-logo-slug";
import { formatMatchTime } from "../../../utils/format-date";

interface MatchListProps {
  matches: MatchListItem[];
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
  return new Date(matchDate).toISOString().slice(0, 10);
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
  const todayUtc = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  const dateUtc = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate());
  const dayDiff = Math.floor((dateUtc - todayUtc) / (1000 * 60 * 60 * 24));

  if (dayDiff <= 0) return null;
  if (dayDiff === 1) return "in 1 day";
  return `in ${dayDiff} days`;
}

export function MatchList({ matches }: MatchListProps) {
  if (matches.length === 0) {
    return (
      <p className="text-center text-zinc-500 dark:text-zinc-400 py-8">
        No matches found.
      </p>
    );
  }

  const groups = matches.reduce<MatchDateGroup[]>((acc, match) => {
    const key = toDateKey(match.matchDate);
    const existing = acc[acc.length - 1];
    if (existing && existing.key === key) {
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
                <span className="inline-block min-w-22 text-center text-2xl font-bold tabular-nums tracking-tight text-foreground">
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
              const showChips =
                match.hasResearch || match.hasResearchBet || match.isReadyToPredict;
              const readinessChips = [
                { key: "preview", label: "Preview", isReady: match.hasPreview },
                { key: "lineup", label: "Lineup", isReady: match.hasLineup },
                { key: "odds", label: "Odds", isReady: match.hasOdds },
                { key: "h2h", label: "H2H", isReady: match.hasHeadToHead },
              ];
              return (
                <li key={match.id} className="bg-white dark:bg-zinc-950">
                  <Link
                    href={`/match/${match.id}`}
                    className="flex flex-col gap-1.5 px-4 py-3 transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900"
                  >
                    {match.leagueName || match.leagueSlug ? (
                      <div className="flex min-w-0 items-center justify-center gap-1.5">
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
                      </div>
                    ) : null}
                    <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-2">
                      <div className="flex min-w-0 items-center justify-end gap-2">
                        <span className="min-w-0 truncate text-end font-medium text-foreground">
                          {match.homeClubName}
                        </span>
                        <SlugIcon
                          kind="club"
                          slug={homeLogoSlug}
                          alt={match.homeClubName}
                          className="h-7 w-7"
                        />
                      </div>
                      <div className="flex justify-center px-1">{centerCell}</div>
                      <div className="flex min-w-0 items-center justify-start gap-2">
                        <SlugIcon
                          kind="club"
                          slug={awayLogoSlug}
                          alt={match.awayClubName}
                          className="h-7 w-7"
                        />
                        <span className="min-w-0 truncate font-medium text-foreground">
                          {match.awayClubName}
                        </span>
                      </div>
                    </div>
                    {showChips ? (
                      <div className="flex flex-wrap items-center justify-center gap-2 pt-0.5">
                        {match.hasResearch ? (
                          <span className="inline-flex items-center rounded-md bg-violet-100 px-2 py-0.5 text-xs font-medium text-violet-800 ring-1 ring-inset ring-violet-600/20 dark:bg-violet-900/40 dark:text-violet-400 dark:ring-violet-500/30">
                            Research
                          </span>
                        ) : null}
                        {match.hasResearchBet ? (
                          <span className="inline-flex items-center rounded-md bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800 ring-1 ring-inset ring-amber-600/20 dark:bg-amber-900/40 dark:text-amber-400 dark:ring-amber-500/30">
                            Research bet
                          </span>
                        ) : null}
                        {match.isReadyToPredict ? (
                          <span className="inline-flex items-center rounded-md bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30">
                            Ready to predict
                          </span>
                        ) : null}
                      </div>
                    ) : null}
                    <div className="flex flex-wrap items-center justify-center gap-1.5 pt-0.5">
                      {readinessChips.map((chip) => (
                        <span
                          key={chip.key}
                          className={chip.isReady
                            ? "inline-flex items-center rounded-md bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30"
                            : "inline-flex items-center rounded-md bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-700 ring-1 ring-inset ring-zinc-400/30 dark:bg-zinc-900/60 dark:text-zinc-300 dark:ring-zinc-600/40"}
                        >
                          {chip.label}
                        </span>
                      ))}
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
        );
      })}
    </div>
  );
}
