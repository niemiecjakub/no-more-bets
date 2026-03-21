import { SlugIcon } from "@/components/slug-icon";
import type { LeagueTableDto } from "../interfaces";

interface LeagueTableProps {
  data: LeagueTableDto;
}

export function LeagueTable({ data }: LeagueTableProps) {
  if (data.rows.length === 0) {
    return (
      <p className="py-8 text-center text-zinc-500 dark:text-zinc-400">
        No table data.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      <table className="w-full min-w-[640px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900">
            <th className="px-3 py-2 text-left font-medium text-foreground">#</th>
            <th className="px-3 py-2 text-left font-medium text-foreground">Team</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">P</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">W</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">D</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">L</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">GF</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">GA</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">GD</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">Pts</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">xG</th>
            <th className="px-3 py-2 text-center font-medium text-foreground">xPts</th>
          </tr>
        </thead>
        <tbody>
          {data.rows.map((row) => (
            <tr
              key={row.clubId}
              className="border-b border-zinc-200 bg-white last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950 dark:last:border-b-0"
            >
              <td className="px-3 py-2 text-zinc-600 dark:text-zinc-400">{row.position}</td>
              <td className="px-3 py-2 font-medium text-foreground">
                <span className="flex items-center gap-2">
                  <SlugIcon
                    kind="club"
                    slug={row.clubSlug}
                    alt={row.clubName}
                    className="h-5 w-5"
                  />
                  {row.clubName}
                </span>
              </td>
              <td className="px-3 py-2 text-center text-foreground">{row.matchesPlayed}</td>
              <td className="px-3 py-2 text-center text-foreground">{row.wins}</td>
              <td className="px-3 py-2 text-center text-foreground">{row.draws}</td>
              <td className="px-3 py-2 text-center text-foreground">{row.losses}</td>
              <td className="px-3 py-2 text-center text-foreground">{row.goalsFor}</td>
              <td className="px-3 py-2 text-center text-foreground">{row.goalsAgainst}</td>
              <td className="px-3 py-2 text-center text-foreground">
                {row.goalDifference >= 0 ? `+${row.goalDifference}` : row.goalDifference}
              </td>
              <td className="px-3 py-2 text-center font-semibold text-foreground">{row.points}</td>
              <td className="px-3 py-2 text-center text-zinc-600 dark:text-zinc-400">
                {row.xg.toFixed(1)}
              </td>
              <td className="px-3 py-2 text-center text-zinc-600 dark:text-zinc-400">
                {row.xpts.toFixed(1)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
