export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <div className="mb-6 h-8 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="animate-pulse space-y-3 rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden">
          {[1, 2, 3, 4, 5, 6, 7].map((i) => (
            <div
              key={i}
              className="h-14 px-4 flex items-center gap-4 bg-white dark:bg-zinc-950"
            >
              <div className="h-4 flex-1 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-4 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
