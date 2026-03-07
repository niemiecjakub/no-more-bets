import type { MatchListItem } from "../interfaces";
import { MATCH_STATUS } from "../interfaces";
import { formatMatchDate } from "../../../utils/format-date";

interface MatchListProps {
  matches: MatchListItem[];
}

function formatScore(match: MatchListItem): string {
  if (match.matchStatusId === MATCH_STATUS.Finished && match.homeGoals != null && match.awayGoals != null) {
    return `${match.homeGoals} - ${match.awayGoals}`;
  }
  return "";
}

export function MatchList({ matches }: MatchListProps) {
  if (matches.length === 0) {
    return (
      <p className="text-center text-zinc-500 dark:text-zinc-400 py-8">
        No matches found.
      </p>
    );
  }

  return (
    <ul className="divide-y divide-zinc-200 dark:divide-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden">
      {matches.map((match) => {
        const score = formatScore(match);
        return (
        <li
          key={match.id}
          className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 bg-white dark:bg-zinc-950 hover:bg-zinc-50 dark:hover:bg-zinc-900 transition-colors"
        >
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
        </li>
        );
      })}
    </ul>
  );
}
