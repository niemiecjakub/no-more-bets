"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const tabs = [
  { href: "/", label: "Matches" },
  { href: "/bets", label: "Bets" },
  { href: "/leagues", label: "Leagues" },
  { href: "/memories", label: "Memories" },
  { href: "/sessions", label: "Sessions" },
] as const;

export function Navbar() {
  const pathname = usePathname();

  return (
    <nav className="sticky top-0 z-50 border-b border-zinc-200 bg-white/90 backdrop-blur supports-backdrop-filter:bg-white/75 dark:border-zinc-800 dark:bg-zinc-950/90 dark:supports-backdrop-filter:bg-zinc-950/75">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <div className="flex items-center justify-between gap-3 py-3">
          <div className="flex min-w-0 flex-1 gap-1 overflow-x-auto">
            {tabs.map(({ href, label }) => {
              const isActive = href === "/" ? pathname === "/" : pathname.startsWith(href);
              return (
                <Link
                  key={href}
                  href={href}
                  className={`shrink-0 rounded-md px-4 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? "bg-zinc-100 dark:bg-zinc-800 text-foreground"
                      : "text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-zinc-900 hover:text-foreground"
                  }`}
                >
                  {label}
                </Link>
              );
            })}
          </div>

          <div className="flex shrink-0 items-center gap-1">
            <a
              href="https://github.com/niemiecjakub/no-more-bets"
              target="_blank"
              rel="noreferrer noopener"
              aria-label="Open no-more-bets GitHub repository"
              title="GitHub"
              className="rounded-md p-2 text-zinc-700 transition-colors hover:bg-zinc-100 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 dark:text-zinc-200 dark:hover:bg-zinc-800"
            >
              <img src="/navbar/github.svg" alt="" className="h-7 w-7 dark:invert" />
            </a>
            <a
              href="https://x.com/nomorebetsai"
              target="_blank"
              rel="noreferrer noopener"
              aria-label="Open no-more-bets profile on X"
              title="X"
              className="rounded-md p-2 text-zinc-700 transition-colors hover:bg-zinc-100 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 dark:text-zinc-200 dark:hover:bg-zinc-800"
            >
              <img src="/navbar/x-icon.svg" alt="" className="h-7 w-7 dark:invert" />
            </a>
          </div>
        </div>
      </div>
    </nav>
  );
}
