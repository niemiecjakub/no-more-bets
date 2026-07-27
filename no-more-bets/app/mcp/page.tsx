import type { Metadata } from "next";
import type { LucideIcon } from "lucide-react";
import {
  Shield,
  Sparkles,
  Swords,
  Trophy,
  Wrench,
} from "lucide-react";
import { fetchMcpToolGroups } from "@/features/mcp/services/mcp-api";
import { McpAccessNote } from "./_components/mcp-access-note";

export const metadata: Metadata = {
  description:
    "Model Context Protocol tools for football matches, clubs, and leagues — research fixtures, odds, lineups, and standings from your AI client.",
};

const groupPresentation: Record<
  string,
  { accent: string; Icon: LucideIcon }
> = {
  matches: { accent: "bg-emerald-500/80", Icon: Swords },
  clubs: { accent: "bg-sky-500/80", Icon: Shield },
  leagues: { accent: "bg-violet-500/80", Icon: Trophy },
};

const fallbackPresentation = {
  accent: "bg-zinc-400/80",
  Icon: Wrench,
};

export default async function McpPage() {
  const toolGroups = await fetchMcpToolGroups();

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
          <McpAccessNote />
        </div>
      </section>

      <section className="mb-12 sm:mb-16">
        <header className="mb-5 flex items-end justify-between gap-4">
          <h2 className="text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
            Tool groups
          </h2>
        </header>
        <div className="grid gap-3 md:grid-cols-3">
          {toolGroups.map(({ id, label, description }) => {
            const { accent, Icon } =
              groupPresentation[id] ?? fallbackPresentation;
            return (
              <a
                key={id}
                href={`#${id}`}
                className="relative flex flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white p-5 transition-colors hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950 dark:hover:bg-zinc-900"
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
              </a>
            );
          })}
        </div>
      </section>

      {toolGroups.map((group) => (
        <section
          key={group.id}
          id={group.id}
          className="mb-12 scroll-mt-20 sm:mb-16"
        >
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
        </section>
      ))}
    </main>
  );
}
