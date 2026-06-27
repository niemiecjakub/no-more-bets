"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import * as NavigationMenu from "@radix-ui/react-navigation-menu";
import { cn } from "@/lib/utils";
import { FeedbackSheetTrigger } from "@/features/feedback/components/feedback-sheet";

const tabs = [
  { href: "/", label: "Matches" },
  { href: "/knowledge", label: "Knowledge" },
  { href: "/about", label: "About" },
] as const;

const agentTabs = [
  { href: "/agent?widget=bankroll", label: "Bankroll", widget: "bankroll" },
  { href: "/agent?widget=summary", label: "Summary", widget: "summary" },
  { href: "/agent?widget=pending", label: "Bets", widget: "pending" },
  { href: "/agent?widget=sessions", label: "Sessions", widget: "sessions" },
  { href: "/agent?widget=memories", label: "Memories", widget: "memories" },
] as const;

function NavbarLinks({
  pathname,
  isAgentRoute,
}: {
  pathname: string;
  isAgentRoute: boolean;
}) {
  return (
    <NavigationMenu.Root delayDuration={80} skipDelayDuration={120}>
      <NavigationMenu.List className="flex items-center gap-1">
        <NavigationMenu.Item>
          <NavigationMenu.Link asChild active={isAgentRoute}>
            <Link
              href="/agent"
              className={cn(
                "shrink-0 rounded-md px-4 py-2 text-sm font-bold transition-colors",
                isAgentRoute
                  ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                  : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
              )}
            >
              Agent
            </Link>
          </NavigationMenu.Link>
        </NavigationMenu.Item>

        {tabs.map(({ href, label }) => {
          const isActive = href === "/" ? pathname === "/" : pathname.startsWith(href);
          return (
            <NavigationMenu.Item key={href}>
              <NavigationMenu.Link asChild active={isActive}>
                <Link
                  href={href}
                  className={cn(
                    "shrink-0 rounded-md px-4 py-2 text-sm font-bold transition-colors",
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
      </NavigationMenu.List>
    </NavigationMenu.Root>
  );
}

function SocialLinks() {
  return (
    <>
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
    </>
  );
}

export function Navbar() {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const activeAgentWidget = searchParams.get("widget");
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
    <nav className="sticky top-0 z-50 overflow-hidden border-b border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex items-center gap-3 px-4 py-3 sm:px-6">
        <button
          type="button"
          aria-label={isMobileMenuOpen ? "Close navigation menu" : "Open navigation menu"}
          aria-expanded={isMobileMenuOpen}
          aria-controls="mobile-nav-menu"
          onClick={() => setIsMobileMenuOpen((current) => !current)}
          className="-ml-2 shrink-0 rounded-md p-2 text-zinc-700 transition-colors hover:bg-zinc-100 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 dark:text-zinc-200 dark:hover:bg-zinc-800 lg:hidden"
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

        <Link
          href="/"
          className="sixtyfour-convergence-logo shrink-0 whitespace-nowrap text-base leading-none text-red-400 transition-opacity hover:opacity-80 sm:text-lg lg:text-xl dark:text-red-400"
        >
          no more bets
        </Link>

        <div className="hidden min-h-0 min-w-0 flex-1 lg:block">
          <div className="mx-auto flex w-full max-w-7xl justify-start overflow-x-auto overflow-y-hidden [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden lg:overflow-visible">
            <NavbarLinks pathname={pathname} isAgentRoute={isAgentRoute} />
          </div>
        </div>

        <div className="ml-auto flex shrink-0 items-center gap-1">
          <FeedbackSheetTrigger className="rounded-md p-2 text-zinc-700 transition-colors hover:bg-zinc-100 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-zinc-400 dark:text-zinc-200 dark:hover:bg-zinc-800 [&_svg]:size-7" />
          <SocialLinks />
        </div>
      </div>
      <div
        id="mobile-nav-menu"
        className={`fixed bottom-0 left-0 right-0 top-16 z-40 transition-[visibility] duration-300 lg:hidden ${
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
            <div className="pt-2">
              <Link
                href="/agent"
                onClick={() => setIsMobileMenuOpen(false)}
                className={`block rounded-md px-4 py-2 text-sm font-bold transition-colors ${
                  pathname.startsWith("/agent")
                    ? "bg-zinc-100 text-foreground dark:bg-zinc-800"
                    : "text-zinc-600 hover:bg-zinc-50 hover:text-foreground dark:text-zinc-400 dark:hover:bg-zinc-900"
                }`}
              >
                Agent
              </Link>
              <div className="mt-1 space-y-1 pl-3">
                {agentTabs.map(({ href, label, widget }) => {
                  const isActive =
                    pathname.startsWith("/agent") &&
                    (activeAgentWidget === widget ||
                      (activeAgentWidget === null && widget === "bankroll"));
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

            {tabs.map(({ href, label }) => {
              const isActive = href === "/" ? pathname === "/" : pathname.startsWith(href);
              return (
                <Link
                  key={`mobile-${href}`}
                  href={href}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className={`block rounded-md px-4 py-2 text-sm font-bold transition-colors ${
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
