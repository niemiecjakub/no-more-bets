"use client";

import { SlugIcon } from "@/components/slug-icon";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import type { LeagueTableDto } from "../interfaces";

interface LeagueTableProps {
  data: LeagueTableDto;
}

function StatHeader({ label, title }: { label: string; title: string }) {
  return (
    <th className="px-3 py-2 text-center font-medium text-foreground">
      <Tooltip>
        <TooltipTrigger
          render={(props) => (
            <span
              {...props}
              className={cn(
                "inline-flex cursor-default rounded px-1.5 py-0.5 font-medium underline decoration-muted-foreground decoration-dotted underline-offset-[5px] outline-none transition-colors hover:bg-muted hover:decoration-transparent focus-visible:ring-2 focus-visible:ring-ring/50",
                props.className,
              )}
            >
              {label}
            </span>
          )}
        />
        <TooltipContent>
          <p>{title}</p>
        </TooltipContent>
      </Tooltip>
    </th>
  );
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
            <StatHeader label="P" title="Matches played" />
            <StatHeader label="W" title="Wins" />
            <StatHeader label="D" title="Draws" />
            <StatHeader label="L" title="Losses" />
            <StatHeader label="GF" title="Goals for" />
            <StatHeader label="GA" title="Goals against" />
            <StatHeader label="GD" title="Goal difference" />
            <StatHeader label="Pts" title="Points" />
            <StatHeader label="xG" title="Expected goals" />
            <StatHeader label="xPts" title="Expected points" />
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
