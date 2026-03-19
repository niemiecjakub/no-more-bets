import Link from "next/link";
import type { MatchListItem } from "../interfaces";
import { MATCH_STATUS } from "../interfaces";
import { formatMatchDate } from "../../../utils/format-date";

interface MatchListProps {
  matches: MatchListItem[];
}

interface MatchDateGroup {
  key: string;
  date: Date;
  matches: MatchListItem[];
}

function formatScore(match: MatchListItem): string {
  if (match.matchStatusId === MATCH_STATUS.Finished && match.homeGoals != null && match.awayGoals != null) {
    return `${match.homeGoals} - ${match.awayGoals}`;
  }
  return "";
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
              const score = formatScore(match);
              const rowContent = (
                <>
                  <div className="min-w-0 flex-1">
                    <span className="font-medium text-foreground">
                      {match.homeClubName}
                    </span>
                    <span className="mx-2 text-zinc-500 dark:text-zinc-400">vs</span>
                    <span className="font-medium text-foreground">
                      {match.awayClubName}
                    </span>
                  </div>
                  <div className="flex items-center gap-4 shrink-0">
                    {match.hasAnalysis ? (
                      <span className="inline-flex items-center rounded-md bg-violet-100 px-2 py-0.5 text-xs font-medium text-violet-800 ring-1 ring-inset ring-violet-600/20 dark:bg-violet-900/40 dark:text-violet-400 dark:ring-violet-500/30">
                        Analysis
                      </span>
                    ) : null}
                    {match.isReadyToPredict && !match.hasAnalysis ? (
                      <span className="inline-flex items-center rounded-md bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-800 ring-1 ring-inset ring-emerald-600/20 dark:bg-emerald-900/40 dark:text-emerald-400 dark:ring-emerald-500/30">
                        Ready to predict
                      </span>
                    ) : null}
                    {score ? (
                      <span className="font-semibold tabular-nums">{score}</span>
                    ) : null}
                    <time
                      dateTime={match.matchDate}
                      className="text-sm text-zinc-600 dark:text-zinc-400 tabular-nums"
                    >
                      {formatMatchDate(match.matchDate)}
                    </time>
                  </div>
                </>
              );
              return (
                <li
                  key={match.id}
                  className="flex flex-wrap items-center justify-between gap-2 bg-white px-4 py-3 transition-colors hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                  <Link href={`/match/${match.id}`} className="contents">
                    {rowContent}
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
