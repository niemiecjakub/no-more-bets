export default function Loading() {
  return (
    <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
      <header className="mb-8 flex gap-3">
        <div className="h-16 w-16 shrink-0 animate-pulse rounded-full bg-zinc-200 dark:bg-zinc-800" />
        <div className="flex min-w-0 flex-col justify-center gap-2 py-0.5">
          <div className="h-8 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="flex items-center gap-2">
            <div className="h-5 w-5 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          </div>
        </div>
      </header>
      <div className="space-y-6">
        {[1, 2, 3, 4, 5].map((i) => (
          <section
            key={i}
            className="overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800"
          >
            <div className="h-11 animate-pulse border-b border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900/50" />
            <div className="space-y-2 p-4">
              {[1, 2, 3].map((row) => (
                <div key={row} className="h-12 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
              ))}
            </div>
          </section>
        ))}
      </div>
    </main>
  );
}
