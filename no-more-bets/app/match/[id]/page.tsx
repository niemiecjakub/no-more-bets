"use client";

import { notFound } from "next/navigation";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect } from "react";
import { StructuredMatchAnalysisView } from "../../../features/matches/components/structured-match-analysis-view";
import { useMatchStore } from "@/store/match-store";
import { formatMatchDate } from "../../../utils/format-date";

function LoadingSkeleton() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <div className="mb-4 h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mb-1 h-7 w-48 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="mb-6 h-4 w-32 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" />
        <div className="space-y-4">
          {[1, 2].map((i) => (
            <div
              key={i}
              className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 overflow-hidden"
            >
              <div className="h-10 border-b border-zinc-200 dark:border-zinc-800 bg-zinc-100 dark:bg-zinc-900/50" />
              <div className="h-24 px-4 py-3" />
            </div>
          ))}
        </div>
      </main>
    </div>
  );
}

export default function MatchPage() {
  const params = useParams();
  const id = params?.id as string | undefined;
  const matchId = id != null && id !== "" ? Number(id) : NaN;
  const isValidId = !Number.isNaN(matchId) && matchId >= 1;

  const {
    matchAnalysisById,
    isLoading,
    error,
    setMatchAnalysisPage,
  } = useMatchStore();

  const data = isValidId ? matchAnalysisById[matchId] : undefined;

  useEffect(() => {
    if (!isValidId) return;
    setMatchAnalysisPage(matchId);
  }, [matchId, isValidId, setMatchAnalysisPage]);

  if (id != null && id !== "" && !isValidId) {
    notFound();
  }

  if (error?.includes("404")) {
    notFound();
  }

  if (isLoading && !data) {
    return <LoadingSkeleton />;
  }

  if (error) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
        <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
          <Link
            href="/"
            className="mb-4 inline-block text-sm text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
          >
            ← Back to matches
          </Link>
          <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
            {error}
          </p>
        </main>
      </div>
    );
  }

  if (!data) {
    return null;
  }

  const matchDateFormatted = formatMatchDate(data.matchDate);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-3xl px-4 py-8 sm:px-6">
        <Link
          href="/"
          className="mb-4 inline-block text-sm text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
        >
          ← Back to matches
        </Link>
        <h1 className="mb-1 text-2xl font-semibold tracking-tight text-foreground">
          {data.homeClubName}
          <span className="mx-2 text-zinc-500 dark:text-zinc-400">vs</span>
          {data.awayClubName}
        </h1>
        <p className="mb-6 text-sm text-zinc-500 dark:text-zinc-400">
          {matchDateFormatted}
        </p>

        {data.analyses.length === 0 ? (
          <p className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-4 py-6 text-center text-zinc-500 dark:text-zinc-400">
            No analyses yet.
          </p>
        ) : (
          <ul className="space-y-6">
            {data.analyses.map((analysis) => (
              <li
                key={analysis.id}
                className="rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 overflow-hidden"
              >
                <h2 className="border-b border-zinc-200 dark:border-zinc-800 px-4 py-3 text-base font-semibold text-foreground">
                  {analysis.code}
                </h2>
                <div className="px-4 pb-4 pt-2">
                  {analysis.structured ? (
                    <StructuredMatchAnalysisView analysis={analysis.structured} />
                  ) : (
                    <p className="text-sm text-zinc-500 dark:text-zinc-400">
                      Analysis not available.
                    </p>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
