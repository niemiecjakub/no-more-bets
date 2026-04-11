export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <div className="mb-6 h-8 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="animate-pulse space-y-3 overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
          {[1, 2, 3, 4, 5, 6, 7].map((i) => (
            <div key={i} className="h-12 bg-white px-4 dark:bg-zinc-950">
              <div className="h-4 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
