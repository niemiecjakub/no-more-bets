import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import type { RecentMatch } from "../interfaces";
import { clubLogoSlugSegment } from "@/utils/club-logo-slug";
import { matchPath } from "@/lib/paths";

function ResultBadge({ result }: { result: string }) {
  const className =
    result === "Win"
      ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300"
      : result === "Loss"
        ? "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
        : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300";
  return <span className={`inline-flex rounded px-2 py-0.5 text-xs font-medium ${className}`}>{result}</span>;
}

function GameRowContent({ game }: { game: RecentMatch }) {
  return (
    <>
      <OpponentInfo game={game} />
      <div className="flex shrink-0 items-center justify-end gap-2">
        <p className="font-semibold tabular-nums text-foreground">{game.score}</p>
        <ResultBadge result={game.result} />
      </div>
    </>
  );
}

function OpponentInfo({ game }: { game: RecentMatch }) {
  return (
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
  );
}

export function RecentGamesList({ games }: { games?: RecentMatch[] | null }) {
  if (!games || games.length === 0) {
    return <p className="text-sm text-zinc-500 dark:text-zinc-400">No recent games available.</p>;
  }

  const rowClass =
    "flex items-center justify-between gap-2 rounded-md border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-950";

  return (
    <div className="space-y-2">
    <ul className="flex flex-col gap-2 text-sm">
      {games.map((game) => (
        <li key={`${game.matchId}-${game.opponent}-${game.date}`}>
          {game.matchId > 0 ? (
            <Link
              href={matchPath({ id: game.matchId })}
              className={`${rowClass} transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900`}
            >
              <GameRowContent game={game} />
            </Link>
          ) : (
            <div className={rowClass}>
              <GameRowContent game={game} />
            </div>
          )}
        </li>
      ))}
    </ul>
    </div>
  );
}
