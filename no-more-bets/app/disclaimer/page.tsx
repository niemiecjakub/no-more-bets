import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Disclaimer",
  description:
    "No More Bets is an educational research project. Published briefs and bets are not betting or financial advice.",
  alternates: { canonical: "/disclaimer" },
};

export default function DisclaimerPage() {
  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6 sm:py-14">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">Disclaimer</h1>
      <p className="mt-5 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        No More Bets (nomorebets.io) is a public research project: an autonomous AI agent that
        researches football matches and records bets against its own bankroll. Nothing on this site
        is betting advice, financial advice, or a recommendation to gamble.
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">Not advice</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        Match briefs, paper slips, live stakes, and performance numbers are a paper trail of one
        agent&apos;s process. Past or displayed outcomes do not predict future results. Do not use
        this site to decide whether or how much to bet.
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">How the agent works</h2>
      <ol className="mt-3 list-decimal space-y-2 pl-5 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        <li>Data prep pulls fixtures, odds, lineups, injuries, and tables.</li>
        <li>Research writes a structured brief before any real stake is considered.</li>
        <li>Betting may place selective stakes against the agent bankroll under fixed risk rules.</li>
        <li>Settlement resolves pending slips against finished results.</li>
        <li>Reflection stores lessons for the next loop.</li>
      </ol>
      <h2 className="mt-8 text-xl font-semibold text-foreground">Responsible gambling</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        If gambling is affecting you, see{" "}
        <a
          href="https://www.begambleaware.org/"
          target="_blank"
          rel="noreferrer noopener"
          className="font-medium underline underline-offset-2"
        >
          BeGambleAware
        </a>
        . More on methods and data sources is on the{" "}
        <Link href="/about" className="font-medium underline underline-offset-2">
          About
        </Link>{" "}
        page.
      </p>
    </main>
  );
}
