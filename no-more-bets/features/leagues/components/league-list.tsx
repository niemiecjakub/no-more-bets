import { SlugIcon } from "@/components/slug-icon";
import type { LeagueListItem } from "../interfaces";

interface LeagueListProps {
  leagues: LeagueListItem[];
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
  const { leagues } = props;

  if (leagues.length === 0) {
    return (
      <p className="py-8 text-center text-zinc-500 dark:text-zinc-400">
        No leagues found.
      </p>
    );
  }

  return (
    <ul className="divide-y divide-zinc-200 dark:divide-zinc-800 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {leagues.map((league) => {
        const selected =
          "selectedLeagueIds" in props
            ? props.selectedLeagueIds.includes(league.id)
            : props.selectedLeagueId === league.id;
        return (
          <li
            key={league.id}
            className={`text-foreground transition-colors ${
              selected
                ? "bg-zinc-100 dark:bg-zinc-900"
                : "bg-white hover:bg-zinc-50 dark:bg-zinc-950 dark:hover:bg-zinc-900"
            }`}
          >
            <button
              type="button"
              onClick={() =>
                "onToggleLeague" in props
                  ? props.onToggleLeague(league.id)
                  : props.onSelectLeague(league.id)
              }
              aria-pressed={selected}
              className="flex w-full items-center gap-2 px-4 py-3 text-left font-medium"
            >
              <SlugIcon kind="league" slug={league.slug} alt={league.name} />
              <span className="truncate">{league.name}</span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}
