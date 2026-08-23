import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Privacy policy",
  description: "How No More Bets handles the little data it collects — feedback, logs, and no non-essential cookies.",
  alternates: { canonical: "/privacy" },
};

export default function PrivacyPage() {
  return (
    <main className="mx-auto w-full max-w-3xl px-4 py-10 sm:px-6 sm:py-14">
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">Privacy policy</h1>
      <p className="mt-5 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        No More Bets (nomorebets.io) is a public research log: match briefs, bankroll, and agent
        sessions. We do not run ads or sell personal data.
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">What we collect</h2>
      <ul className="mt-3 list-disc space-y-2 pl-5 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        <li>
          Optional feedback you send through the in-app form or GitHub issues (whatever you type,
          plus technical context needed to store the message).
        </li>
        <li>
          Standard server logs (IP address, user agent, path, time) for operating and securing the
          site.
        </li>
        <li>MCP access requests, if you ask for API credentials.</li>
      </ul>
      <h2 className="mt-8 text-xl font-semibold text-foreground">Cookies</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        We do not set non-essential cookies for advertising or cross-site tracking. Essential
        cookies or local storage may be used only to keep the app working (for example, UI state).
      </p>
      <h2 className="mt-8 text-xl font-semibold text-foreground">Contact</h2>
      <p className="mt-3 text-base leading-7 text-zinc-600 dark:text-zinc-300">
        Questions: open an issue on{" "}
        <a
          href="https://github.com/niemiecjakub/no-more-bets/issues"
          target="_blank"
          rel="noreferrer noopener"
          className="font-medium underline underline-offset-2"
        >
          GitHub
        </a>{" "}
        or use Feedback in the navbar.
      </p>
    </main>
  );
}
