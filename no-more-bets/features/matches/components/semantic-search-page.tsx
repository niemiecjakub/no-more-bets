"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Input } from "@/components/ui/input";
import { MatchList } from "@/features/matches/components/match-list";
import type { MatchListItem } from "@/features/matches/interfaces";
import { fetchSemanticSearchMatches } from "@/features/matches/services/matches-api";
import { handleServiceError } from "@/lib/error-handler";

function SearchResultsFallback() {
  return (
    <div className="animate-pulse space-y-2">
      <div className="h-4 w-40 rounded bg-zinc-200 dark:bg-zinc-800" />
      <div className="overflow-hidden rounded-lg border border-zinc-200 dark:border-zinc-800">
        {[1, 2, 3].map((row) => (
          <div
            key={row}
            className="space-y-2 border-b border-zinc-200 bg-white px-4 py-3 last:border-b-0 dark:border-zinc-800 dark:bg-zinc-950"
          >
            <div className="mx-auto h-3 w-28 rounded bg-zinc-200 dark:bg-zinc-800" />
            <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-x-3">
              <div className="ml-auto h-6 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-6 w-14 rounded bg-zinc-200 dark:bg-zinc-800" />
              <div className="h-6 w-24 rounded bg-zinc-200 dark:bg-zinc-800" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function SemanticSearchField({
  value,
  onChange,
}: {
  value: string;
  onChange: (value: string) => void;
}) {
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      if (draft !== value) {
        onChange(draft);
      }
    }, 300);

    return () => window.clearTimeout(timeoutId);
  }, [draft, value, onChange]);

  return (
    <Input
      type="search"
      value={draft}
      onChange={(event) => setDraft(event.target.value)}
      placeholder="Describe fixtures, form, injuries, tactics…"
      aria-label="Semantic match search"
      className="h-11 bg-white text-base dark:bg-zinc-950"
      autoFocus
    />
  );
}

export function SemanticSearchPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const query = (searchParams.get("q") ?? "").trim();

  const [matches, setMatches] = useState<MatchListItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleQueryChange = useCallback(
    (value: string) => {
      const params = new URLSearchParams(searchParams.toString());
      const trimmed = value.trim();
      if (trimmed) {
        params.set("q", trimmed);
      } else {
        params.delete("q");
      }
      const next = params.toString();
      router.replace(next ? `${pathname}?${next}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams],
  );

  useEffect(() => {
    if (!query) {
      setMatches([]);
      setError(null);
      setIsLoading(false);
      return;
    }

    let isMounted = true;

    async function load() {
      setIsLoading(true);
      setError(null);
      try {
        const results = await fetchSemanticSearchMatches(query);
        if (!isMounted) return;
        setMatches(results);
      } catch (err) {
        if (!isMounted) return;
        setMatches([]);
        setError(handleServiceError(err, "Failed to search matches."));
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    void load();
    return () => {
      isMounted = false;
    };
  }, [query]);

  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
        <div className="flex flex-col gap-2">
          <h1 className="text-lg font-semibold tracking-tight text-foreground">
            Search matches
          </h1>
          <p className="text-sm text-zinc-500 dark:text-zinc-400">
            Natural-language search across match and research content. Looking
            for a club name? Use{" "}
            <Link
              href="/"
              className="font-medium text-foreground underline-offset-2 hover:underline"
            >
              Matches
            </Link>{" "}
            filters instead.
          </p>
          <SemanticSearchField value={query} onChange={handleQueryChange} />
        </div>

        {!query ? (
          <p className="rounded-lg border border-zinc-200 bg-white px-4 py-3 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
            Try something like “tight underdog fixtures”, “Arsenal injuries this
            weekend”, or “World Cup defensive battles”.
          </p>
        ) : isLoading ? (
          <SearchResultsFallback />
        ) : error ? (
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        ) : matches.length === 0 ? (
          <p className="rounded-lg border border-zinc-200 bg-white px-4 py-3 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
            No related matches found. Try a broader topic query, or search by
            club name on{" "}
            <Link
              href="/"
              className="font-medium text-foreground underline-offset-2 hover:underline"
            >
              Matches
            </Link>
            .
          </p>
        ) : (
          <MatchList matches={matches} />
        )}
      </div>
    </main>
  );
}
