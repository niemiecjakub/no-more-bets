import type { ReactNode } from "react";

export function WidgetCard({
  title,
  value,
  meta,
  accentClassName,
  valueClassName,
  isActive = false,
  onClick,
}: {
  title: string;
  value: ReactNode;
  meta?: ReactNode;
  accentClassName: string;
  valueClassName?: string;
  isActive?: boolean;
  onClick?: () => void;
}) {
  const cardClassName = `relative overflow-hidden rounded-lg border bg-white p-4 text-left transition-colors dark:bg-zinc-950 ${
    isActive
      ? "border-zinc-900 bg-sky-50 dark:border-zinc-100 dark:bg-sky-950/40"
      : "border-zinc-200 hover:border-zinc-400 dark:border-zinc-800 dark:hover:border-zinc-600"
  }`;

  if (onClick) {
    return (
      <button type="button" onClick={onClick} className={cardClassName}>
        <div className={`absolute left-0 top-0 h-1 w-full ${accentClassName}`} />
        <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">{title}</p>
        <div className={`mt-2 text-2xl font-semibold ${valueClassName ?? "text-foreground"}`}>{value}</div>
        {meta ? <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-300">{meta}</div> : null}
      </button>
    );
  }

  return (
    <article className={cardClassName}>
      <div className={`absolute left-0 top-0 h-1 w-full ${accentClassName}`} />
      <p className="text-xs font-medium uppercase tracking-wide text-zinc-500 dark:text-zinc-400">{title}</p>
      <div className={`mt-2 text-2xl font-semibold ${valueClassName ?? "text-foreground"}`}>{value}</div>
      {meta ? <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-300">{meta}</div> : null}
    </article>
  );
}

export function WidgetSkeleton() {
  return (
    <div className="flex flex-col gap-2 animate-pulse">
      <div className="h-8 w-32 rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="h-4 w-48 rounded bg-zinc-200 dark:bg-zinc-800" />
    </div>
  );
}
