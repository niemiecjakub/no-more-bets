import Link from "next/link";

const footerLinks = [
  { href: "/disclaimer", label: "Disclaimer" },
  { href: "/terms", label: "Terms" },
  { href: "/privacy", label: "Privacy" },
] as const;

const linkClassName =
  "underline-offset-2 hover:text-zinc-700 hover:underline dark:hover:text-zinc-300";

export function SiteFooter() {
  return (
    <footer className="fixed inset-x-0 bottom-0 z-30 border-t border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="mx-auto max-w-7xl px-4 py-2.5 sm:px-6">
        <nav aria-label="Footer" className="mb-1.5 flex flex-wrap items-center justify-center gap-x-3 gap-y-1 text-xs text-zinc-600 dark:text-zinc-400">
          {footerLinks.map((link) => (
            <Link key={link.href} href={link.href} className={linkClassName}>
              {link.label}
            </Link>
          ))}
          <a
            href="https://github.com/niemiecjakub/no-more-bets"
            target="_blank"
            rel="noreferrer noopener"
            className={linkClassName}
          >
            GitHub
          </a>
        </nav>
        <p className="text-center text-[11px] leading-relaxed text-zinc-500 dark:text-zinc-500 sm:text-xs">
          Educational research project — not betting or financial advice; past or displayed outcomes do not predict future results. If gambling is affecting you, see{" "}
          <a href="https://www.begambleaware.org/" target="_blank" rel="noreferrer noopener" className="underline underline-offset-2 hover:text-zinc-700 dark:hover:text-zinc-400">
            BeGambleAware
          </a>
          .
        </p>
      </div>
    </footer>
  );
}
