export default function Loading() {
  return (
    <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <div className="mb-6 h-8 w-36 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="grid min-h-[min(70vh,36rem)] animate-pulse grid-cols-1 gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,3fr)]">
          <div className="flex flex-col gap-2 overflow-hidden rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
            {[1, 2, 3, 4].map((i) => (
              <div
                key={i}
                className="rounded-md border border-zinc-100 px-3 py-2.5 dark:border-zinc-800"
              >
                <div className="h-4 w-3/4 rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="mt-2 h-3 w-full rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="mt-2 h-3 w-20 rounded bg-zinc-200 dark:bg-zinc-800" />
              </div>
            ))}
          </div>
          <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
            <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
              <div className="h-6 w-2/3 rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
            <div className="space-y-2 p-4">
              <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
          </div>
        </div>
    </main>
  );
}
