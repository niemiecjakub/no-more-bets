function ToolGroupCardSkeleton() {
  return (
    <div className="relative flex flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="absolute left-0 top-0 h-1 w-full animate-pulse bg-zinc-200 dark:bg-zinc-800" />
      <div className="mb-3 flex items-center gap-3">
        <div className="h-9 w-9 shrink-0 animate-pulse rounded-md bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-5 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
      <div className="mt-2 space-y-2">
        <div className="h-4 w-full animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="h-4 w-4/5 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      </div>
    </div>
  );
}

function ToolItemSkeleton() {
  return (
    <li className="rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex items-start gap-3 rounded-lg p-3">
        <div className="mt-0.5 h-7 w-7 shrink-0 animate-pulse rounded-md bg-zinc-200 dark:bg-zinc-800" />
        <div className="flex-1 space-y-2">
          <div className="h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 w-40 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
          <div className="h-3 w-full animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        </div>
      </div>
    </li>
  );
}

function ToolSectionSkeleton({ toolCount }: { toolCount: number }) {
  return (
    <section className="mb-12 sm:mb-16">
      <div className="h-7 w-36 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
      <ul className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2">
        {Array.from({ length: toolCount }, (_, index) => (
          <ToolItemSkeleton key={index} />
        ))}
      </ul>
    </section>
  );
}

export function McpToolsSkeleton() {
  return (
    <>
      <section className="mb-12 sm:mb-16">
        <header className="mb-5 flex items-end justify-between gap-4">
          <h2 className="text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
            Tool groups
          </h2>
        </header>
        <div className="grid gap-3 md:grid-cols-3">
          <ToolGroupCardSkeleton />
          <ToolGroupCardSkeleton />
          <ToolGroupCardSkeleton />
        </div>
      </section>

      <ToolSectionSkeleton toolCount={4} />
      <ToolSectionSkeleton toolCount={5} />
      <ToolSectionSkeleton toolCount={3} />
    </>
  );
}
