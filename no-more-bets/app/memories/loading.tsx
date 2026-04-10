export default function Loading() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-4xl px-4 py-8 sm:px-6">
        <div className="mb-6 h-8 w-36 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="animate-pulse space-y-4">
          {[1, 2, 3].map((i) => (
            <div
              key={i}
              className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950"
            >
              <div className="border-b border-zinc-100 dark:border-zinc-800 px-4 py-3">
                <div className="h-5 w-48 rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="mt-2 h-3 w-64 rounded bg-zinc-200 dark:bg-zinc-800" />
              </div>
              <div className="space-y-2 px-4 py-3">
                <div className="h-3 max-w-full rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="h-3 max-w-md rounded bg-zinc-200 dark:bg-zinc-800" />
                <div className="h-3 max-w-lg rounded bg-zinc-200 dark:bg-zinc-800" />
              </div>
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}
