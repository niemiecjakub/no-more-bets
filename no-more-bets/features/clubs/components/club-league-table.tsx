"use client";

import Link from "next/link";
import { SlugIcon } from "@/components/slug-icon";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import type { LeagueTable, MatchResult } from "@/features/leagues/interfaces";

function TableHeaderTooltip({
  label,
  tooltip,
  className,
}: {
  label: string;
  tooltip: string;
  className: string;
}) {
  return (
    <th className={className}>
      <Tooltip>
        <TooltipTrigger className="cursor-help border-0 bg-transparent p-0 font-medium text-inherit underline decoration-dotted decoration-zinc-400 underline-offset-2 dark:decoration-zinc-500">
          {label}
        </TooltipTrigger>
        <TooltipContent>{tooltip}</TooltipContent>
      </Tooltip>
    </th>
  );
}

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
      ? "bg-emerald-500 text-white"
      : normalized === "Loss"
        ? "bg-red-500 text-white"
        : "bg-zinc-400 text-white dark:bg-zinc-500";
  return (
    <span
      className={`inline-flex h-6 w-6 items-center justify-center rounded text-xs font-semibold ${className}`}
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
      <table className="min-w-full w-max max-w-full table-fixed text-sm">
        <colgroup>
          <col className="w-11" />
          <col className="w-44" />
          <col className="w-11" />
          <col className="w-11" />
          <col className="w-11" />
          <col className="w-11" />
          <col className="w-11" />
          <col className="w-[9.5rem]" />
        </colgroup>
        <thead>
          <tr className="border-b border-zinc-200 text-xs uppercase tracking-wide text-zinc-500 dark:border-zinc-800 dark:text-zinc-400">
            <TableHeaderTooltip
              label="#"
              tooltip="Position"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <th className="max-w-44 px-2 py-2 text-left font-medium">
              <span className="sr-only">Club</span>
            </th>
            <TableHeaderTooltip
              label="P"
              tooltip="Played"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <TableHeaderTooltip
              label="W"
              tooltip="Wins"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <TableHeaderTooltip
              label="D"
              tooltip="Draws"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <TableHeaderTooltip
              label="L"
              tooltip="Losses"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <TableHeaderTooltip
              label="Pts"
              tooltip="Points"
              className="px-2 py-2 text-center font-medium tabular-nums"
            />
            <TableHeaderTooltip
              label="Form"
              tooltip="Last 5 results"
              className="px-2 py-2 text-center font-medium"
            />
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
                <td className="px-2 py-2 text-center tabular-nums text-zinc-600 dark:text-zinc-300">{row.position}</td>
                <td className="max-w-44 px-2 py-2">
                  <Link
                    href={`/club/${row.clubId}`}
                    className="flex min-w-0 items-center gap-2 font-medium text-foreground transition-colors hover:text-red-600 dark:hover:text-red-400"
                    title={row.clubName}
                  >
                    <SlugIcon kind="club" slug={row.clubSlug} alt={row.clubName} className="h-6 w-6 shrink-0" />
                    <span className="truncate">{row.clubName}</span>
                  </Link>
                </td>
                <td className="px-2 py-2 text-center tabular-nums text-foreground">{row.matchesPlayed}</td>
                <td className="px-2 py-2 text-center tabular-nums text-foreground">{row.wins}</td>
                <td className="px-2 py-2 text-center tabular-nums text-foreground">{row.draws}</td>
                <td className="px-2 py-2 text-center tabular-nums text-foreground">{row.losses}</td>
                <td className="px-2 py-2 text-center font-semibold tabular-nums text-foreground">{row.points}</td>
                <td className="px-2 py-2">
                  {row.form.length > 0 ? (
                    <div className="flex justify-center gap-1">
                      {row.form.map((result, index) => (
                        <FormLetter key={`${row.clubId}-${index}`} result={result} />
                      ))}
                    </div>
                  ) : null}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
