import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import type { LeagueTable, MatchResult } from "@/features/leagues/interfaces";

const MATCH_RESULT_LABEL: Record<MatchResult, string> = {
  Win: "W",
  Draw: "D",
  Loss: "L",
};

function normalizeMatchResult(result: MatchResult | number): MatchResult {
  if (result === 0 || result === "Win") return "Win";
  if (result === 1 || result === "Draw") return "Draw";
  return "Loss";
}

function FormLetter({ result }: { result: MatchResult | number }) {
  const normalized = normalizeMatchResult(result);
  const letter = MATCH_RESULT_LABEL[normalized];
  const className =
    normalized === "Win"
      ? "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300"
      : normalized === "Loss"
        ? "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300"
        : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300";
  return (
    <span
      className={`inline-flex h-5 w-5 items-center justify-center rounded text-[10px] font-semibold ${className}`}
    >
      {letter}
    </span>
  );
}

interface ClubLeagueTableProps {
  table: LeagueTable;
  highlightClubId: number;
}

export function ClubLeagueTable({ table, highlightClubId }: ClubLeagueTableProps) {
  if (table.rows.length === 0) {
    return <p className="text-sm text-zinc-500 dark:text-zinc-400">No league table data available.</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[32rem] text-sm">
        <thead>
          <tr className="border-b border-zinc-200 text-left text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800 dark:text-zinc-400">
            <th className="px-3 py-2 font-medium">#</th>
            <th className="px-3 py-2 font-medium">Club</th>
            <th className="px-3 py-2 font-medium tabular-nums">P</th>
            <th className="px-3 py-2 text-center font-medium tabular-nums">W</th>
            <th className="px-3 py-2 text-center font-medium tabular-nums">D</th>
            <th className="px-3 py-2 text-center font-medium tabular-nums">L</th>
            <th className="px-3 py-2 font-medium">Form</th>
            <th className="px-3 py-2 font-medium tabular-nums">Pts</th>
          </tr>
        </thead>
        <tbody>
          {table.rows.map((row) => {
            const highlighted = row.clubId === highlightClubId;
            return (
              <tr
                key={row.clubId}
                className={
                  highlighted
                    ? "border-b border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-800/60"
                    : "border-b border-zinc-200 dark:border-zinc-800"
                }
              >
                <td className="px-3 py-2 tabular-nums text-zinc-600 dark:text-zinc-300">{row.position}</td>
                <td className="px-3 py-2">
                  <Link
                    href={`/club/${row.clubId}`}
                    className="flex min-w-0 items-center gap-2 font-medium text-foreground hover:underline"
                  >
                    <SlugIcon kind="club" slug={row.clubSlug} alt={row.clubName} className="h-6 w-6" />
                    <span className="truncate">{row.clubName}</span>
                  </Link>
                </td>
                <td className="px-3 py-2 tabular-nums text-foreground">{row.matchesPlayed}</td>
                <td className="px-3 py-2 text-center tabular-nums text-foreground">{row.wins}</td>
                <td className="px-3 py-2 text-center tabular-nums text-foreground">{row.draws}</td>
                <td className="px-3 py-2 text-center tabular-nums text-foreground">{row.losses}</td>
                <td className="px-3 py-2">
                  {row.form.length > 0 ? (
                    <div className="flex gap-0.5">
                      {row.form.map((result, index) => (
                        <FormLetter key={`${row.clubId}-${index}`} result={result} />
                      ))}
                    </div>
                  ) : null}
                </td>
                <td className="px-3 py-2 font-semibold tabular-nums text-foreground">{row.points}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
