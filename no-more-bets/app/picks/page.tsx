import type { Metadata } from "next";
import { JsonLd } from "@/components/json-ld";
import { DailyPicksList } from "@/features/bets/components/daily-picks-list";
import { getDailyPicksPage } from "@/features/bets/services/bets-server";
import type { BetSlipListItem } from "@/features/bets/interfaces";
import type { PagedResponse } from "@/lib/paged-response";
import { softwareApplicationNode } from "@/lib/schema";
import { absoluteUrl } from "@/lib/site";

export const revalidate = 60;

export const metadata: Metadata = {
  title: "Daily picks",
  description: "House daily slips by date — Low, Medium, and High paper bets for each card.",
  alternates: { canonical: "/picks" },
  openGraph: {
    title: "Daily picks",
    description: "House daily slips by date — Low, Medium, and High paper bets for each card.",
    url: "/picks",
  },
};

const emptyPage: PagedResponse<BetSlipListItem> = {
  items: [],
  hasMore: false,
  nextCursorAt: null,
  nextCursorId: null,
};

export default async function PicksPage() {
  let page = emptyPage;
  let loadFailed = false;
  try {
    page = await getDailyPicksPage({ limit: 7 });
  } catch {
    loadFailed = true;
  }

  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [softwareApplicationNode(absoluteUrl("/picks"))],
        }}
      />
      {loadFailed ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
          Could not load daily picks.
        </p>
      ) : (
        <DailyPicksList initialPage={page} />
      )}
    </main>
  );
}
