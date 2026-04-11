export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <div className="mb-1 h-8 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
          <div className="space-y-0 border-b border-zinc-200 dark:border-zinc-800">
            {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
              <div
                key={i}
                className="flex h-12 items-center gap-4 border-b border-zinc-200 bg-white px-3 last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950"
              >
                <div className="h-4 w-6 rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="h-4 flex-1 max-w-[12rem] rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="h-4 w-8 rounded bg-zinc-200 dark:bg-zinc-800" />
              </div>
            ))}
          </div>
        </div>
      </main>
    </div>
  );
}
