import { Suspense } from "react";
import { LeagueList } from "../../features/leagues/components/league-list";
import { fetchLeagues } from "../../features/leagues/services/leagues-api";

async function LeaguesContent() {
  try {
    const leagues = await fetchLeagues();
    return <LeagueList leagues={leagues} />;
  } catch (err) {
    const message = err instanceof Error ? err.message : "Failed to load leagues.";
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {message}
      </p>
    );
  }
}

function LeaguesFallback() {
  return (
    <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="h-12 bg-white px-4 dark:bg-zinc-950">
          <div className="h-4 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function LeaguesPage() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Leagues
        </h1>
        <Suspense fallback={<LeaguesFallback />}>
          <LeaguesContent />
        </Suspense>
      </main>
    </div>
  );
}
