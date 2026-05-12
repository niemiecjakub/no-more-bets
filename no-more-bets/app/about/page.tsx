import type { Metadata } from "next";
import Link from "next/link";
import {
  BookOpen,
  Brain,
  Calendar,
  Globe,
  MessagesSquare,
  ShieldAlert,
  Sparkles,
  Target,
  Wallet,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { AgentProcessTab } from "@/app/agent/_components/agent-process-tab";

export const metadata: Metadata = {
  title: "About · No More Bets",
  description:
    "An autonomous AI agent that researches football matches, places real bets against its own bankroll, and reflects on outcomes — operating in public on a daily schedule.",
};

const capabilities = [
  {
    title: "Researches matches",
    description:
      "Builds a structured view of each fixture before any decision is made.",
    accent: "bg-emerald-500/80",
    Icon: BookOpen,
    linkHref: "/",
    linkLabel: "See matches",
    LinkIcon: Calendar,
  },
  {
    title: "Places real bets",
    description:
      "Places selective bets with strict discipline and clear risk boundaries.",
    accent: "bg-sky-500/80",
    Icon: Target,
    linkHref: "/agent?widget=pending",
    linkLabel: "See agent bets",
    LinkIcon: Wallet,
  },
  {
    title: "Reflects to improve",
    description:
      "Reviews outcomes and continuously improves future decision quality.",
    accent: "bg-violet-500/80",
    Icon: Brain,
    linkHref: "/agent?widget=sessions",
    linkLabel: "See agent sessions",
    LinkIcon: MessagesSquare,
  },
] as const;

const capabilityCtaClassName =
  "border-zinc-200 bg-zinc-50 text-zinc-900 hover:bg-zinc-100 dark:border-zinc-700 dark:bg-zinc-900/40 dark:text-zinc-100 dark:hover:bg-zinc-800/80";

const dataSources = [
  {
    name: "Soccerdata",
    role: "Fixtures, finished scores, head-to-head",
    href: "https://soccerdataapi.com/",
  },
  {
    name: "Betclic",
    role: "Bookmaker listings & odds",
    href: "https://www.betclic.pl/",
  },
  {
    name: "Fotmob",
    role: "Match details, lineups, ratings",
    href: "https://www.fotmob.com/",
  },
  {
    name: "Rotowire",
    role: "Injuries & availability",
    href: "https://www.rotowire.com/soccer/",
  },
] as const;

export default function AboutPage() {
  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-12 sm:px-6">
        <section className="mb-14">
          <div>
            <p className="mb-4 inline-flex items-center gap-2 rounded-full border border-zinc-200 bg-white px-3 py-1 text-xs font-medium uppercase tracking-[0.2em] text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300">
              <Sparkles className="h-3.5 w-3.5" aria-hidden />
              Autonomous research and betting agent
            </p>
            <h1 className="text-balance text-4xl font-semibold tracking-tight text-foreground sm:text-5xl">
              An AI Agent that researches football, places its own bets, and
              learns from the result.
            </h1>
            <p className="mt-5 text-balance text-base leading-7 text-zinc-600 dark:text-zinc-300 sm:text-lg">
              No More Bets explores autonomous football decision-making in the
              open. The agent follows a repeatable daily cycle to research
              matches, take measured action, and learn from outcomes over time.
            </p>
            <div className="mt-7 flex flex-wrap gap-3">
              <Link
                href="/agent"
                className="inline-flex items-center gap-2 rounded-md bg-zinc-900 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:-translate-y-0.5 hover:bg-zinc-800 hover:shadow-md dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200"
              >
                Open Agent Dashboard
              </Link>
              <Link
                href="/"
                className="inline-flex items-center gap-2 rounded-md border border-zinc-200 bg-white px-5 py-2.5 text-sm font-medium text-zinc-800 transition-colors hover:bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200 dark:hover:bg-zinc-900"
              >
                Browse Matches
              </Link>
            </div>
          </div>
        </section>

        <section className="mb-16">
          <header className="mb-5 flex items-end justify-between gap-4">
            <h2 className="text-2xl font-semibold tracking-tight text-foreground">
              How it works
            </h2>
          </header>
          <div className="grid gap-3 md:grid-cols-3">
            {capabilities.map(
              ({
                title,
                description,
                accent,
                Icon,
                linkHref,
                linkLabel,
                LinkIcon: CtaIcon,
              }) => (
              <article
                key={title}
                className="relative flex flex-col overflow-hidden rounded-lg border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-zinc-950"
              >
                <div className={`absolute left-0 top-0 h-1 w-full ${accent}`} />
                <div className="mb-3 flex items-center gap-3">
                  <div className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200">
                    <Icon className="h-5 w-5" aria-hidden />
                  </div>
                  <h3 className="text-base font-semibold text-foreground">
                    {title}
                  </h3>
                </div>
                <p className="mt-2 flex-1 text-sm leading-6 text-zinc-600 dark:text-zinc-300">
                  {description}
                </p>
                <Link
                  href={linkHref}
                  className={cn(
                    "mt-4 inline-flex w-fit items-center gap-2 rounded-md border px-3 py-2 text-xs font-semibold transition-colors sm:text-sm",
                    capabilityCtaClassName
                  )}
                >
                  <CtaIcon className="h-4 w-4 shrink-0" aria-hidden />
                  {linkLabel}
                </Link>
              </article>
            ))}
          </div>
        </section>

        <div className="mb-16">
          <h2 className="text-2xl font-semibold tracking-tight text-foreground">
            Internal process
          </h2>
          <div className="mt-5">
            <AgentProcessTab />
          </div>
        </div>

        <section className="mb-16">
          <div>
            <h2 className="mt-1 text-2xl font-semibold tracking-tight text-foreground">
              Data sources
            </h2>
            <ul className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2">
              {dataSources.map((source) => (
                <li
                  key={source.name}
                  className="rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950"
                >
                  <a
                    href={source.href}
                    target="_blank"
                    rel="noreferrer noopener"
                    className="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-zinc-100 dark:hover:bg-zinc-900"
                  >
                    <span className="mt-0.5 inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-zinc-100 text-zinc-700 dark:bg-zinc-900 dark:text-zinc-200">
                      <Globe className="h-4 w-4" aria-hidden />
                    </span>
                    <div>
                      <p className="text-sm font-semibold text-foreground">
                        {source.name}
                      </p>
                      <p className="text-xs text-zinc-600 dark:text-zinc-300">
                        {source.role}
                      </p>
                    </div>
                  </a>
                </li>
              ))}
            </ul>
          </div>
        </section>

        <section className="rounded-xl border border-amber-200 bg-amber-50 p-5 dark:border-amber-900/60 dark:bg-amber-950/30">
          <div className="flex items-start gap-3">
            <span className="mt-0.5 inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-200">
              <ShieldAlert className="h-5 w-5" aria-hidden />
            </span>
            <div className="space-y-2 text-sm leading-6 text-amber-900 dark:text-amber-100">
              <p className="text-base font-semibold">
                Educational and research project
              </p>
              <p>
                No More Bets explores whether a disciplined LLM agent with
                persistent memory can find structural edge in football betting
                markets, with its process and outcomes visible over time. It is
                not financial advice. Bet results are inherently variable;
                nothing here implies future profit.
              </p>
              <p className="flex items-center gap-2 text-amber-900/90 dark:text-amber-200/90">
                <Wallet className="h-4 w-4" aria-hidden />
                If gambling is hurting you, please reach out to{" "}
                <a
                  href="https://www.begambleaware.org/"
                  target="_blank"
                  rel="noreferrer noopener"
                  className="underline-offset-2 hover:underline"
                >
                  BeGambleAware
                </a>{" "}
                or your local responsible-gambling service.
              </p>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
