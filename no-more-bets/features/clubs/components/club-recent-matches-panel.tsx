import { RecentGamesList } from "@/features/matches/components/recent-games-list";
import type { RecentMatch } from "@/features/matches/interfaces";

interface ClubRecentMatchesPanelProps {
  games?: RecentMatch[] | null;
}

export function ClubRecentMatchesPanel({ games }: ClubRecentMatchesPanelProps) {
  return (
    <div className="bg-zinc-50/70 px-4 py-4 dark:bg-zinc-900/35">
      <RecentGamesList games={games} />
    </div>
  );
}
