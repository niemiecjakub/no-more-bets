export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <div className="mb-6 h-8 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="grid gap-8 lg:grid-cols-[1fr_18rem] lg:items-start">
          <div>
            <div className="animate-pulse space-y-4">
              {[1, 2, 3, 4].map((i) => (
                <div
                  key={i}
                  className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950"
                >
                  <div className="flex gap-2 px-4 py-3 border-b border-zinc-100 dark:border-zinc-800">
                    <div className="h-5 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-4 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                  <div className="grid grid-cols-3 gap-3 px-4 py-3 border-b border-zinc-100 dark:border-zinc-800">
                    <div className="h-8 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-8 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-8 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                  <div className="space-y-2 px-4 py-3">
                    <div className="h-4 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-4 max-w-xs rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-3 max-w-sm rounded bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                </div>
              ))}
            </div>
          </div>
          <aside className="lg:sticky lg:top-8">
            <div className="animate-pulse overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
            <div className="space-y-3 border-b border-zinc-100 p-4 dark:border-zinc-800">
              <div className="h-4 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-8 w-32 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-3 w-40 rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
            <div className="p-4">
              <div className="mb-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
              {[1, 2, 3].map((i) => (
                <div
                  key={i}
                  className="border-b border-zinc-100 py-3 last:border-0 dark:border-zinc-800/80"
                >
                  <div className="flex justify-between gap-2">
                    <div className="h-4 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
                    <div className="h-4 w-16 rounded bg-zinc-200 dark:bg-zinc-800" />
                  </div>
                  <div className="mt-2 h-3 w-40 rounded bg-zinc-200 dark:bg-zinc-800" />
                </div>
              ))}
            </div>
            </div>
          </aside>
        </div>
      </main>
    </div>
  );
}
