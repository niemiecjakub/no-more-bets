import type { Metadata } from "next";
import Link from "next/link";

export const metadata: Metadata = {
  title: "Terms of use",
  description:
    "Terms for using nomorebets.io: public research content, no betting advice, and MCP access on request.",
  alternates: { canonical: "/terms" },
};

export default function TermsPage() {
  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6 sm:py-14">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">Terms of use</h1>
      <p className="mt-5 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        By using nomorebets.io you agree that the site is an educational research project, not a
        licensed bookmaker, tipster service, or financial product.
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">The content</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        Match research, slips, sessions, and bankroll figures are published as a paper trail. They
        may be wrong, incomplete, or delayed. You use them at your own risk. See the{" "}
        <Link href="/disclaimer" className="font-medium underline underline-offset-2">
          disclaimer
        </Link>
        .
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">MCP and APIs</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        Football data tools over Model Context Protocol are available on request. Access may be
        rate-limited, revoked, or changed without notice. Do not treat tool output as advice.
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">Acceptable use</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        Do not scrape in a way that harms the service, impersonate the operator, or use the project
        to promote unlicensed gambling. Fixture data comes from third-party sources listed on{" "}
        <Link href="/about" className="font-medium underline underline-offset-2">
          About
        </Link>
        ; their terms also apply to that data.
      </p>
    </main>
  );
}
