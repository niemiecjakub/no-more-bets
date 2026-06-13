"use client";

import { ClubLeagueTable } from "@/features/clubs/components/club-league-table";
import type { LeagueTable } from "@/features/leagues/interfaces";
import { cn } from "@/lib/utils";

interface ClubWorldCupGroupTablesProps {
  table: LeagueTable;
  highlightClubId: number;
}

export function ClubWorldCupGroupTables({ table, highlightClubId }: ClubWorldCupGroupTablesProps) {
  const groups = table.groups ?? [];

  if (groups.length === 0) {
    return <ClubLeagueTable table={table} highlightClubId={highlightClubId} />;
  }

  return (
    <div className="divide-y divide-zinc-200 dark:divide-zinc-800">
      {groups.map((group) => {
        const isOwnGroup = group.groupCode === table.ownGroupCode;
        return (
          <section key={group.groupCode} className={cn(isOwnGroup && "bg-zinc-50/80 dark:bg-zinc-900/40")}>
            <div
              className={cn(
                "border-b border-zinc-200 px-4 py-2.5 dark:border-zinc-800",
                isOwnGroup && "bg-zinc-100/90 dark:bg-zinc-900/70",
              )}
            >
              <h3 className="text-sm font-semibold text-foreground">{group.groupLabel}</h3>
            </div>
            <ClubLeagueTable
              table={{ ...table, rows: group.rows }}
              highlightClubId={highlightClubId}
            />
          </section>
        );
      })}
    </div>
  );
}
