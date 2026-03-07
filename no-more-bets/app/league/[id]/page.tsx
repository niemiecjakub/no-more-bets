import { notFound } from "next/navigation";
import { LeagueTable } from "../../../features/leagues/components/league-table";
import { fetchLeagueTable } from "../../../features/leagues/services/leagues-api";

interface LeaguePageProps {
  params: Promise<{ id: string }>;
}

export default async function LeaguePage({ params }: LeaguePageProps) {
  const { id } = await params;
  const leagueId = Number(id);
  if (Number.isNaN(leagueId) || leagueId < 1) {
    notFound();
  }

  let leagueTable;
  try {
    leagueTable = await fetchLeagueTable(leagueId);
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    if (message.includes("404")) {
      notFound();
    }
    throw err;
  }

  const snapshotDate = leagueTable.snapshotDate
    ? new Date(leagueTable.snapshotDate).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
      })
    : null;

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-1 text-2xl font-semibold tracking-tight text-foreground">
          {leagueTable.leagueName}
        </h1>
        {snapshotDate && (
          <p className="mb-6 text-sm text-zinc-500 dark:text-zinc-400">
            Table as of {snapshotDate}
          </p>
        )}
        {!snapshotDate && <div className="mb-6" />}
        <LeagueTable data={leagueTable} />
      </main>
    </div>
  );
}
