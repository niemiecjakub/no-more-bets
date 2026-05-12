export function SiteFooter() {
    return (
        <footer className="border-t border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
            <div className="mx-auto max-w-7xl px-4 py-3 sm:px-6">
                <p className="text-center text-xs leading-relaxed text-zinc-500 dark:text-zinc-500">
                    Educational research project - not betting or financial advice; past or displayed outcomes do not predict future results. If gambling is affecting you, see{" "}
                    <a href="https://www.begambleaware.org/" target="_blank" rel="noreferrer noopener" className="underline underline-offset-2 hover:text-zinc-700 dark:hover:text-zinc-400">
                        BeGambleAware
                    </a>
                    .
                </p>
            </div>
        </footer>
    );
}
