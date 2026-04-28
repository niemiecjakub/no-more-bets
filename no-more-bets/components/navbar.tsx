"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import * as NavigationMenu from "@radix-ui/react-navigation-menu";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const tabs = [
  { href: "/", label: "Matches" },
  { href: "/leagues", label: "Leagues" },
] as const;

const agentTabs = [
  { href: "/agent?tab=bets", label: "Bets", tab: "bets" },
  { href: "/agent?tab=sessions", label: "Sessions", tab: "sessions" },
  { href: "/agent?tab=memories", label: "Memories", tab: "memories" },
] as const;

export function Navbar() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const activeAgentTab = searchParams.get("tab");
  const isAgentRoute = pathname.startsWith("/agent");

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
            <div className="hidden min-w-0 flex-1 sm:flex">
              <NavigationMenu.Root delayDuration={80} skipDelayDuration={120}>
                <NavigationMenu.List className="flex items-center gap-1">
                  {tabs.map(({ href, label }) => {
                    const isActive = href === "/" ? pathname === "/" : pathname.startsWith(href);
                    return (
                      <NavigationMenu.Item key={href}>
                        <NavigationMenu.Link asChild active={isActive}>
                          <Link
                            href={href}
                            className={cn(
                              "shrink-0 rounded-md px-4 py-2 text-sm font-medium transition-colors",
                              isActive
                                ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                                : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                            )}
                          >
                            {label}
                          </Link>
                        </NavigationMenu.Link>
                      </NavigationMenu.Item>
                    );
                  })}

                  <NavigationMenu.Item className="relative">
                    <NavigationMenu.Trigger
                      className={cn(
                        "inline-flex items-center gap-1 rounded-md px-4 py-2 text-sm font-medium transition-colors",
                        isAgentRoute
                          ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                          : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                      )}
                    >
                      Agent
                      <ChevronDown className="h-4 w-4 transition-transform data-[state=open]:rotate-180" />
                    </NavigationMenu.Trigger>

                    <NavigationMenu.Content className="absolute left-0 top-full z-50 mt-1 w-44 rounded-md border border-zinc-200 bg-white p-1 shadow-lg dark:border-zinc-800 dark:bg-zinc-950">
                      <ul className="space-y-1">
                        {agentTabs.map(({ href, label, tab }) => {
                          const isActive = isAgentRoute && activeAgentTab === tab;
                          return (
                            <li key={href}>
                              <NavigationMenu.Link asChild active={isActive}>
                                <Link
                                  href={href}
                                  className={cn(
                                    "block rounded-md px-3 py-2 text-sm transition-colors",
                                    isActive
                                      ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                                      : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                                  )}
                                >
                                  {label}
                                </Link>
                              </NavigationMenu.Link>
                            </li>
                          );
                        })}
                      </ul>
                    </NavigationMenu.Content>
                  </NavigationMenu.Item>
                </NavigationMenu.List>
              </NavigationMenu.Root>
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

            <div className="pt-2">
              <Link
                href="/agent"
                onClick={() => setIsMobileMenuOpen(false)}
                className={`block rounded-md px-4 py-2 text-sm font-medium transition-colors ${
                  pathname.startsWith("/agent")
                    ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                    : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                }`}
              >
                Agent
              </Link>
              <div className="mt-1 space-y-1 pl-3">
                {agentTabs.map(({ href, label, tab }) => {
                  const isActive = pathname.startsWith("/agent") && activeAgentTab === tab;
                  return (
                    <Link
                      key={`mobile-${href}`}
                      href={href}
                      onClick={() => setIsMobileMenuOpen(false)}
                      className={`block rounded-md px-4 py-2 text-sm transition-colors ${
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
        </div>
      </div>
    </nav>
  );
}
