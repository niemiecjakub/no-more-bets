import { SlugIcon } from "@/components/slug-icon";
import { cn } from "@/lib/utils";
import type { LeagueListItem } from "../interfaces";

interface LeagueListProps {
  leagues: LeagueListItem[];
  className?: string;
}

type MultiSelectProps = LeagueListProps & {
  selectedLeagueIds: number[];
  onToggleLeague: (id: number) => void;
};

type SingleSelectProps = LeagueListProps & {
  selectedLeagueId: number | null;
  onSelectLeague: (id: number) => void;
};

type Props = MultiSelectProps | SingleSelectProps;

export function LeagueList(props: Props) {
  const { leagues, className } = props;

  if (leagues.length === 0) {
    return (
      <p className="py-8 text-center text-zinc-500 dark:text-zinc-400">
        No leagues found.
      </p>
    );
  }

  return (
    <div
      className={cn(
        "flex flex-col divide-y divide-zinc-200 dark:divide-zinc-800",
        className,
      )}
    >
      {leagues.map((league) => {
        const selected =
          "selectedLeagueIds" in props
            ? props.selectedLeagueIds.includes(league.id)
            : props.selectedLeagueId === league.id;
        return (
          <button
            key={league.id}
            type="button"
            onClick={() =>
              "onToggleLeague" in props
                ? props.onToggleLeague(league.id)
                : props.onSelectLeague(league.id)
            }
            aria-pressed={selected}
            className={cn(
              "m-0 block w-full border-0 px-3 py-3 text-left transition-colors",
              selected
                ? "bg-zinc-100 dark:bg-zinc-800"
                : "bg-white hover:bg-zinc-100 dark:bg-zinc-950 dark:hover:bg-zinc-800",
            )}
          >
            <div className="flex items-center gap-3">
              <SlugIcon kind="league" slug={league.slug} alt={league.name} />
              <span className="truncate text-sm font-semibold text-foreground">
                {league.name}
              </span>
            </div>
          </button>
        );
      })}
    </div>
  );
}
