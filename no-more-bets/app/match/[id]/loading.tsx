export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <div className="mb-1 h-8 w-64 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mb-6 h-4 w-40 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="space-y-6">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="rounded-lg border border-zinc-200 dark:border-zinc-800 overflow-hidden"
            >
              <div className="h-6 w-48 bg-zinc-200 dark:bg-zinc-800 animate-pulse mx-4 mt-4 rounded" />
              <div className="h-32 mx-4 my-4 bg-zinc-200 dark:bg-zinc-800 animate-pulse rounded" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
