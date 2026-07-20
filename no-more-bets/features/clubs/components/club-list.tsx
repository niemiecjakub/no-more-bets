import { SlugIcon } from "@/components/slug-icon";
import type { ClubListItem } from "../interfaces";

interface ClubListProps {
  clubs: ClubListItem[];
}

export function ClubList({ clubs }: ClubListProps) {
  if (clubs.length === 0) {
    return (
      <p className="py-8 text-center text-zinc-500 dark:text-zinc-400">
        No clubs found.
      </p>
    );
  }

  return (
    <ul className="divide-y divide-zinc-200 dark:divide-zinc-800 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {clubs.map((club) => (
        <li
          key={club.id}
          className="flex flex-wrap items-center justify-between gap-2 bg-white px-4 py-3 transition-colors hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900"
        >
          <span className="flex min-w-0 items-center gap-2 font-medium text-foreground">
            <SlugIcon kind="club" slug={club.slug} alt={club.name} />
            <span className="truncate">{club.name}</span>
          </span>
          <span className="flex flex-wrap items-center justify-end gap-3 text-sm text-zinc-600 dark:text-zinc-400">
            {club.memberships.length > 0 ? (
              club.memberships.map((membership) => (
                <span key={membership.seasonId} className="flex items-center gap-2">
                  <SlugIcon kind="league" slug={membership.leagueSlug} alt={membership.leagueName} />
                  <span className="truncate">
                    {membership.leagueName} {membership.seasonYear}
                  </span>
                </span>
              ))
            ) : (
              <span>No season memberships</span>
            )}
          </span>
        </li>
      ))}
    </ul>
  );
}
