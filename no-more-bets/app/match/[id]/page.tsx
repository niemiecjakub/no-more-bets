import { notFound } from "next/navigation";
import Link from "next/link";
import { MatchAnalysisContent } from "../../../features/matches/components/match-analysis-content";
import { fetchMatchAnalysisPage } from "../../../features/matches/services/matches-api";
import { formatMatchDate } from "../../../utils/format-date";

interface MatchPageProps {
  params: Promise<{ id: string }>;
}

export default async function MatchPage({ params }: MatchPageProps) {
  const { id } = await params;
  const matchId = Number(id);
  if (Number.isNaN(matchId) || matchId < 1) {
    notFound();
  }

  let data;
  try {
    data = await fetchMatchAnalysisPage(matchId);
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    if (message.includes("404") || message.includes("Failed to fetch")) {
      notFound();
    }
    throw err;
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
                  <MatchAnalysisContent content={analysis.content} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </main>
    </div>
  );
}
