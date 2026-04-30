import { SlugIcon } from "@/components/slug-icon";
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

const leagueAccentByName: Record<string, string> = {
  "premier league": "#3D195B",
  "serie a": "#004C97",
  ekstraklasa: "#001E4B",
  "ligue 1": "#001E90",
  "la liga": "#FF4B44",
  bundesliga: "#D3010C",
};

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
    <div className={className ?? "grid grid-cols-1 gap-0"}>
      {leagues.map((league) => {
        const isFirst = leagues[0]?.id === league.id;
        const isLast = leagues[leagues.length - 1]?.id === league.id;
        const selected =
          "selectedLeagueIds" in props
            ? props.selectedLeagueIds.includes(league.id)
            : props.selectedLeagueId === league.id;
        const accentColor =
          leagueAccentByName[league.name.toLowerCase()] ??
          "rgb(228 228 231 / 1)";
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
            className={`relative overflow-hidden border bg-white px-3.5 py-3 text-left transition-colors dark:bg-zinc-950 ${
              isFirst ? "rounded-t-lg" : ""
            } ${isLast ? "rounded-b-lg" : ""} ${
              selected
                ? "border-zinc-500 !bg-zinc-100 ring-2 ring-zinc-400/35 dark:border-zinc-500 dark:!bg-zinc-800 dark:ring-zinc-300/20"
                : "border-zinc-200 hover:border-zinc-400 hover:bg-zinc-100 dark:border-zinc-800 dark:hover:border-zinc-600 dark:hover:bg-zinc-800"
            }`}
          >
            <div
              className="absolute left-0 top-0 h-full w-1"
              style={{ backgroundColor: accentColor }}
            />
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
