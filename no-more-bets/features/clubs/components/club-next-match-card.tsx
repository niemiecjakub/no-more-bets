import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import type { ClubNextMatch } from "../interfaces";
import { clubLogoSlugSegment } from "@/utils/club-logo-slug";
import { formatMatchTime } from "@/utils/format-date";

interface ClubNextMatchCardProps {
  match: ClubNextMatch;
  leagueName?: string;
  leagueSlug?: string;
}

export function ClubNextMatchCard({ match, leagueName, leagueSlug }: ClubNextMatchCardProps) {
  const homeLogoSlug = clubLogoSlugSegment(match.homeClubSlug, match.homeClubName);
  const awayLogoSlug = clubLogoSlugSegment(match.awayClubSlug, match.awayClubName);
  const venueLabel = match.isHome ? "Home" : "Away";
  const showLeague = Boolean(leagueName || leagueSlug);

  return (
    <Link
      href={`/match/${match.matchId}`}
      className="flex flex-col gap-1.5 px-4 py-3 transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-900"
    >
      {showLeague ? <LeagueRow leagueName={leagueName} leagueSlug={leagueSlug} /> : null}
      <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-2">
        <div className="flex min-w-0 items-center justify-end gap-2">
          <span className="min-w-0 truncate text-end font-medium text-foreground">{match.homeClubName}</span>
          <SlugIcon kind="club" slug={homeLogoSlug} alt={match.homeClubName} className="h-7 w-7" />
        </div>
        <time
          dateTime={match.matchDate}
          className="inline-block min-w-22 text-center text-2xl font-bold tabular-nums tracking-tight text-foreground"
        >
          {formatMatchTime(match.matchDate)}
        </time>
        <div className="flex min-w-0 items-center justify-start gap-2">
          <SlugIcon kind="club" slug={awayLogoSlug} alt={match.awayClubName} className="h-7 w-7" />
          <span className="min-w-0 truncate font-medium text-foreground">{match.awayClubName}</span>
        </div>
      </div>
      <div className="flex justify-center pt-0.5">
        <span className="inline-flex items-center rounded-md bg-zinc-100 px-2 py-0.5 text-xs font-medium text-zinc-700 ring-1 ring-inset ring-zinc-400/30 dark:bg-zinc-900/60 dark:text-zinc-300 dark:ring-zinc-600/40">
          {venueLabel}
        </span>
      </div>
    </Link>
  );
}

function LeagueRow({ leagueName, leagueSlug }: { leagueName?: string; leagueSlug?: string }) {
  return (
    <div className="flex min-w-0 items-center justify-center gap-1.5">
      <SlugIcon kind="league" slug={leagueSlug ?? ""} alt={leagueName || "League"} className="h-4 w-4" />
      {leagueName ? (
        <p className="min-w-0 truncate text-xs font-medium text-zinc-500 dark:text-zinc-400">{leagueName}</p>
      ) : null}
    </div>
  );
}
