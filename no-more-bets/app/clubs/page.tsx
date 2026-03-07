import { Suspense } from "react";
import { ClubList } from "../../features/clubs/components/club-list";
import { fetchClubs } from "../../features/clubs/services/clubs-api";

async function ClubsContent() {
  try {
    const clubs = await fetchClubs();
    return <ClubList clubs={clubs} />;
  } catch (err) {
    const message = err instanceof Error ? err.message : "Failed to load clubs.";
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {message}
      </p>
    );
  }
}

function ClubsFallback() {
  return (
    <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
      {[1, 2, 3, 4, 5].map((i) => (
        <div key={i} className="flex h-14 items-center gap-4 bg-white px-4 dark:bg-zinc-950">
          <div className="h-4 flex-1 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      ))}
    </div>
  );
}

export default function ClubsPage() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <h1 className="mb-6 text-2xl font-semibold tracking-tight text-foreground">
          Clubs
        </h1>
        <Suspense fallback={<ClubsFallback />}>
          <ClubsContent />
        </Suspense>
      </main>
    </div>
  );
}
