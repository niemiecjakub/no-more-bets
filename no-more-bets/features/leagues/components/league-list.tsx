import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import type { LeagueListItem } from "../interfaces";

interface LeagueListProps {
  leagues: LeagueListItem[];
}

export function LeagueList({ leagues }: LeagueListProps) {
  if (leagues.length === 0) {
    return (
      <p className="py-8 text-center text-zinc-500 dark:text-zinc-400">
        No leagues found.
      </p>
    );
  }

  return (
    <ul className="divide-y divide-zinc-200 dark:divide-zinc-800 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {leagues.map((league) => (
        <li
          key={league.id}
          className="bg-white text-foreground transition-colors hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900"
        >
          <Link
            href={`/league/${league.id}`}
            className="flex items-center gap-2 px-4 py-3 font-medium"
          >
            <SlugIcon kind="league" slug={league.slug} alt={league.name} />
            <span className="truncate">{league.name}</span>
          </Link>
        </li>
      ))}
    </ul>
  );
}
