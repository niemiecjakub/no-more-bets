import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Page not found",
  robots: { index: false, follow: true },
};

export default function NotFound() {
  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">Page not found</h1>
      <p className="mt-3 max-w-xl text-base leading-7 text-zinc-600 dark:text-zinc-300">
        That URL is not a match, club, or page on No More Bets. Check the link, or go back to the
        fixture list.
      </p>
      <p className="mt-6">
        <Link
          href="/"
          className="inline-flex items-center rounded-md bg-zinc-900 px-4 py-2 text-sm font-semibold text-white hover:bg-zinc-800 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
        >
          Browse matches
        </Link>
      </p>
    </main>
  );
}
