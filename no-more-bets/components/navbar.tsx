"use client";

import { useEffect, useState } from "react";
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
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  useEffect(() => {
    if (!isMobileMenuOpen) return;

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setIsMobileMenuOpen(false);
      }
    }

    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", onKeyDown);

    return () => {
      document.body.style.overflow = "";
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isMobileMenuOpen]);

  useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [pathname]);

  return (
    <nav className="sticky top-0 z-50 border-b border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <div className="flex items-center justify-between gap-3 py-3">
          <div className="flex min-w-0 flex-1 items-center">
            <button
              type="button"
              aria-label={isMobileMenuOpen ? "Close navigation menu" : "Open navigation menu"}
              aria-expanded={isMobileMenuOpen}
              aria-controls="mobile-nav-menu"
              onClick={() => setIsMobileMenuOpen((current) => !current)}
              className="rounded-md p-2 text-zinc-700 transition-colors hover:bg-zinc-100 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 dark:text-zinc-200 dark:hover:bg-zinc-800 sm:hidden"
            >
              <span className="sr-only">
                {isMobileMenuOpen ? "Close navigation menu" : "Open navigation menu"}
              </span>
              <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                className="h-7 w-7"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              >
                {isMobileMenuOpen ? (
                  <path d="M6 6L18 18M18 6L6 18" />
                ) : (
                  <>
                    <path d="M4 7H20" />
                    <path d="M4 12H20" />
                    <path d="M4 17H20" />
                  </>
                )}
              </svg>
            </button>
            <div className="hidden min-w-0 flex-1 gap-1 overflow-x-auto sm:flex">
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

          <div className="ml-auto flex shrink-0 items-center gap-1">
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
      <div
        id="mobile-nav-menu"
        className={`fixed bottom-0 left-0 right-0 top-16 z-40 transition-[visibility] duration-300 sm:hidden ${
          isMobileMenuOpen ? "visible" : "invisible"
        }`}
        aria-hidden={!isMobileMenuOpen}
      >
        <button
          type="button"
          aria-label="Close navigation menu"
          onClick={() => setIsMobileMenuOpen(false)}
          className={`absolute inset-0 bg-zinc-950/15 transition-all duration-300 ${
            isMobileMenuOpen ? "opacity-100 backdrop-blur-sm" : "opacity-0 backdrop-blur-0"
          }`}
        />
        <div
          className={`absolute bottom-0 left-0 top-0 w-72 border-r border-zinc-200 bg-white p-4 shadow-xl transition-transform duration-300 ease-out dark:border-zinc-800 dark:bg-zinc-950 ${
            isMobileMenuOpen ? "translate-x-0" : "-translate-x-full"
          }`}
        >
          <div className="space-y-1">
            {tabs.map(({ href, label }) => {
              const isActive = href === "/" ? pathname === "/" : pathname.startsWith(href);
              return (
                <Link
                  key={`mobile-${href}`}
                  href={href}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className={`block rounded-md px-4 py-2 text-sm font-medium transition-colors ${
                    isActive
                      ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                      : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                  }`}
                >
                  {label}
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    </nav>
  );
}
