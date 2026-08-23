import type { Metadata } from "next";
import HomePage from "../_components/home-page";
import { JsonLd } from "@/components/json-ld";
import { MATCH_STATUS } from "@/features/matches/interfaces";
import { ALL_STATUSES_ID, parseSortOrderParam, type FetchMatchesFilters } from "@/features/matches/services/matches-api";
import { getMatchesPage } from "@/features/matches/services/matches-server";
import { getLeagues, getSeasonYears } from "@/features/leagues/services/leagues-server";
import type { LeagueListItem } from "@/features/leagues/interfaces";
import type { MatchListItem } from "@/features/matches/interfaces";
import type { PagedResponse } from "@/lib/paged-response";
import { matchPath } from "@/lib/paths";
import { softwareApplicationNode } from "@/lib/schema";
import { absoluteUrl, DEFAULT_DESCRIPTION, DEFAULT_TITLE } from "@/lib/site";

export const revalidate = 60;

type HomeSearchParams = {
  status?: string;
  leagues?: string;
  season?: string;
  search?: string;
  sort?: string;
  afterDate?: string;
  afterId?: string;
};

function parseLeagueIds(raw: string | undefined): number[] {
  if (!raw) return [];
  return raw
    .split(",")
    .map((item) => Number(item.trim()))
    .filter((id) => Number.isInteger(id) && id > 0);
}

export async function generateMetadata({
  searchParams,
}: {
  searchParams: Promise<HomeSearchParams>;
}): Promise<Metadata> {
  const sp = await searchParams;
  const hasSearch = Boolean(sp.search?.trim());
  return {
    title: { absolute: DEFAULT_TITLE },
    description: DEFAULT_DESCRIPTION,
    alternates: { canonical: "/" },
    openGraph: {
      title: DEFAULT_TITLE,
      description: DEFAULT_DESCRIPTION,
      url: "/",
    },
    robots: hasSearch ? { index: false, follow: true } : { index: true, follow: true },
  };
}

export default async function Page({
  searchParams,
}: {
  searchParams: Promise<HomeSearchParams>;
}) {
  const sp = await searchParams;
  let leagues: LeagueListItem[] = [];
  let seasonYears: string[] = [];
  let matchPage: PagedResponse<MatchListItem> = {
    items: [],
    hasMore: false,
    nextCursorAt: null,
    nextCursorId: null,
  };

  try {
    const [leagueList, seasons] = await Promise.all([getLeagues(), getSeasonYears()]);
    leagues = leagueList;
    seasonYears = seasons.map((item) => item.year);
  } catch {
    // Client will retry.
  }

  const statusParam = Number(sp.status);
  const selectedStatusId =
    statusParam === ALL_STATUSES_ID || statusParam === MATCH_STATUS.Upcoming || statusParam === MATCH_STATUS.Finished
      ? statusParam
      : MATCH_STATUS.Upcoming;
  const latestSeasonYear = seasonYears[0] ?? null;
  const seasonRaw = sp.season;
  let selectedSeasonYears: string[];
  if (seasonRaw == null) {
    selectedSeasonYears = latestSeasonYear ? [latestSeasonYear] : [];
  } else if (seasonRaw.trim() === "") {
    selectedSeasonYears = [];
  } else {
    selectedSeasonYears = seasonRaw
      .split(",")
      .map((item) => item.trim())
      .filter((year) => seasonYears.includes(year));
    if (selectedSeasonYears.length === 0 && latestSeasonYear) {
      selectedSeasonYears = [latestSeasonYear];
    }
  }

  const filters: FetchMatchesFilters = {
    matchStatusId: selectedStatusId === ALL_STATUSES_ID ? undefined : selectedStatusId,
    leagueIds: parseLeagueIds(sp.leagues),
    sortOrder: parseSortOrderParam(sp.sort ?? null, selectedStatusId),
    search: sp.search?.trim() || undefined,
    seasonYears: selectedSeasonYears.length > 0 ? selectedSeasonYears : undefined,
  };

  const afterId = sp.afterId != null ? Number(sp.afterId) : NaN;
  const cursor =
    sp.afterDate && Number.isInteger(afterId) && afterId > 0
      ? { afterMatchDate: sp.afterDate, afterId }
      : {};

  try {
    matchPage = await getMatchesPage(filters, { ...cursor, limit: 10 });
  } catch {
    // Client will retry.
  }

  const itemList = {
    "@type": "ItemList",
    name: "Upcoming football matches",
    itemListElement: matchPage.items.slice(0, 20).map((match, index) => ({
      "@type": "ListItem",
      position: index + 1,
      url: absoluteUrl(matchPath(match)),
      name: `${match.homeClubName} vs ${match.awayClubName}`,
    })),
  };

  return (
    <>
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [softwareApplicationNode(absoluteUrl("/")), itemList],
        }}
      />
      <HomePage
        initialMatches={matchPage.items}
        initialHasMore={matchPage.hasMore}
        initialCursor={
          matchPage.nextCursorAt != null && matchPage.nextCursorId != null
            ? { at: matchPage.nextCursorAt, id: matchPage.nextCursorId }
            : null
        }
        initialFilters={filters}
        initialLeagues={leagues}
        initialSeasonYears={seasonYears}
      />
    </>
  );
}
