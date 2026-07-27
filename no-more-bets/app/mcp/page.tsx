import type { Metadata } from "next";
import {
  Shield,
  Sparkles,
  Swords,
  Trophy,
  Wrench,
} from "lucide-react";

export const metadata: Metadata = {
  description:
    "Model Context Protocol tools for football matches, clubs, and leagues — research fixtures, odds, lineups, and standings from your AI client.",
};

const toolGroups = [
  {
    id: "matches",
    label: "Matches",
    description:
      "Search fixtures and pull research, lineups, odds, and events.",
    accent: "bg-emerald-500/80",
    Icon: Swords,
    tools: [
      {
        name: "matches_search",
        title: "Search matches",
        description:
          "Browse and resolve fixtures into a matchId. Supports free-text search, league filters, status, and cursor pagination.",
      },
      {
        name: "matches_getResearch",
        title: "Read match research",
        description:
          "Returns the latest stored research for a match. Null when the match has not been researched yet.",
      },
      {
        name: "matches_getCurrentOdds",
        title: "Check current odds",
        description:
          "Latest stored odds — 1X2, BTTS, double chance, over/under by default; optional exotic markets.",
      },
      {
        name: "matches_getLineups",
        title: "Look up lineups",
        description:
          "Starting lineups for both clubs. Null when no lineup has been collected yet.",
      },
      {
        name: "matches_getInjuries",
        title: "Check injuries",
        description:
          "Injured or unavailable players for both clubs of a match.",
      },
      {
        name: "matches_getHeadToHeadStats",
        title: "Review head-to-head",
        description:
          "Aggregated historical head-to-head statistics between the two clubs.",
      },
      {
        name: "matches_getEvents",
        title: "Read match events",
        description:
          "Timeline of goals, cards, and substitutions ordered by minute.",
      },
    ],
  },
  {
    id: "clubs",
    label: "Clubs",
    description:
      "Resolve clubs and inspect form, fixtures, and rolling performance.",
    accent: "bg-sky-500/80",
    Icon: Shield,
    tools: [
      {
        name: "clubs_getList",
        title: "List clubs",
        description:
          "All clubs ordered by name with league/season memberships — use to resolve a clubId.",
      },
      {
        name: "clubs_getById",
        title: "Look up a club",
        description:
          "A single club with its league/season memberships. Null when unknown.",
      },
      {
        name: "clubs_getMatches",
        title: "List club matches",
        description: "All stored matches for a club — past and upcoming.",
      },
      {
        name: "clubs_getNextMatch",
        title: "Find next fixture",
        description:
          "The club's next upcoming match, or null when none is scheduled.",
      },
      {
        name: "clubs_getRecentGames",
        title: "Review recent form",
        description:
          "Last 5 finished matches with opponent, score, and result.",
      },
      {
        name: "clubs_getRollingPerformance",
        title: "Review recent performance",
        description:
          "Player ratings, team ratings, and formations over the last 5 finished matches.",
      },
    ],
  },
  {
    id: "leagues",
    label: "Leagues",
    description: "Standings and club-level league statistics.",
    accent: "bg-violet-500/80",
    Icon: Trophy,
    tools: [
      {
        name: "leagues_getList",
        title: "List leagues",
        description:
          "All known leagues ordered by name — use to resolve a leagueId.",
      },
      {
        name: "leagues_getTable",
        title: "View league table",
        description:
          "Standings with position, points, W/D/L, goals, and expected metrics. Optional as-of date.",
      },
      {
        name: "leagues_getClubStatistics",
        title: "Check club league stats",
        description:
          "One club's league statistics: table position, record, goals, and expected metrics.",
      },
    ],
  },
] as const;

export default function McpPage() {
  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
      <section className="mb-10 sm:mb-14">
        <div>
          <p className="mb-4 inline-flex max-w-full items-center gap-2 rounded-full border border-zinc-200 bg-white px-3 py-1 text-xs font-medium uppercase tracking-[0.15em] text-zinc-600 sm:tracking-[0.2em] dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300">
            <Sparkles className="h-3.5 w-3.5 shrink-0" aria-hidden />
            Model Context Protocol
          </p>
          <h1 className="text-balance text-3xl font-semibold tracking-tight text-foreground sm:text-4xl md:text-5xl">
            Football data tools for your AI client.
          </h1>
          <p className="mt-5 text-balance text-base leading-7 text-zinc-600 dark:text-zinc-300 sm:text-lg">
            Connect No More Bets as an MCP server and give agents structured
            access to matches, clubs, leagues, odds, and research - the same
            data the public agent uses.
          </p>
          <p className="mt-4 text-balance text-base leading-7 text-zinc-500 dark:text-zinc-400 sm:text-lg">
            If you would like access, contact me on{" "}
            <a
              href="https://github.com/niemiecjakub/no-more-bets"
              target="_blank"
              rel="noreferrer noopener"
              className="font-medium text-foreground underline underline-offset-2 hover:text-zinc-700 dark:hover:text-zinc-200"
            >
              GitHub
            </a>
            .
          </p>
        </div>
      </section>

      <section className="mb-12 sm:mb-16">
        <header className="mb-5 flex items-end justify-between gap-4">
          <h2 className="text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
            Tool groups
          </h2>
        </header>
        <div className="grid gap-3 md:grid-cols-3">
          {toolGroups.map(({ label, description, accent, Icon }) => (
            <article
              key={label}
              className="relative flex flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950"
            >
              <div className={`absolute left-0 top-0 h-1 w-full ${accent}`} />
              <div className="mb-3 flex items-center gap-3">
                <div className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200">
                  <Icon className="h-5 w-5" aria-hidden />
                </div>
                <h3 className="text-base font-semibold text-foreground">
                  {label}
                </h3>
              </div>
              <p className="mt-2 flex-1 text-sm leading-6 text-zinc-600 dark:text-zinc-300">
                {description}
              </p>
            </article>
          ))}
        </div>
      </section>

      {toolGroups.map((group) => (
        <section key={group.id} className="mb-12 sm:mb-16">
          <div>
            <h2 className="mt-1 text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
              {group.label}
            </h2>
            <ul className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2">
              {group.tools.map((tool) => (
                <li
                  key={tool.name}
                  className="rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
                >
                  <div className="flex items-start gap-3 rounded-lg p-3">
                    <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200">
                      <Wrench className="h-4 w-4" aria-hidden />
                    </span>
                    <div>
                      <p className="text-sm font-semibold text-foreground">
                        {tool.title}
                      </p>
                      <p className="mt-0.5 font-mono text-xs text-zinc-500 dark:text-zinc-400">
                        {tool.name}
                      </p>
                      <p className="mt-1 text-xs text-zinc-600 dark:text-zinc-300">
                        {tool.description}
                      </p>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        </section>
      ))}
    </main>
  );
}
