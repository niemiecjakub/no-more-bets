"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const tabs = [
  { href: "/", label: "Matches" },
  { href: "/bets", label: "Bets" },
  { href: "/leagues", label: "Leagues" },
  { href: "/clubs", label: "Clubs" },
  { href: "/memories", label: "Memories" },
] as const;

export function Navbar() {
  const pathname = usePathname();

  return (
    <nav className="border-b border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <div className="flex gap-1 overflow-x-auto py-3">
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
      </div>
    </nav>
  );
}
