import type { MetadataRoute } from "next";
import { MATCH_STATUS } from "@/features/matches/interfaces";
import { getClubs } from "@/features/clubs/services/clubs-server";
import { getLeagues } from "@/features/leagues/services/leagues-server";
import {
  getMatchesPage,
  getUpcomingResearchedMatches,
  isIndexableMatch,
} from "@/features/matches/services/matches-server";
import { MATCH_DATE_SORT } from "@/features/matches/services/matches-api";
import { clubPath, leaguePath, matchPath } from "@/lib/paths";
import { absoluteUrl } from "@/lib/site";

export const revalidate = 3600;

const FINISHED_SITEMAP_CAP = 2000;

function entry(path: string, lastModified?: Date, changeFrequency?: MetadataRoute.Sitemap[number]["changeFrequency"], priority?: number): MetadataRoute.Sitemap[number] {
  return {
    url: absoluteUrl(path),
    lastModified,
    changeFrequency,
    priority,
  };
}

async function finishedMatchUrls(): Promise<MetadataRoute.Sitemap> {
  const urls: MetadataRoute.Sitemap = [];
  let afterMatchDate: string | undefined;
  let afterId: number | undefined;

  while (urls.length < FINISHED_SITEMAP_CAP) {
    const page = await getMatchesPage(
      { matchStatusId: MATCH_STATUS.Finished, sortOrder: MATCH_DATE_SORT.Descending },
      { limit: 100, afterMatchDate, afterId },
    );

    for (const match of page.items) {
      if (!isIndexableMatch(match)) continue;
      urls.push(
        entry(
          matchPath(match),
          new Date(match.matchDate),
          "weekly",
          0.7,
        ),
      );
      if (urls.length >= FINISHED_SITEMAP_CAP) break;
    }

    if (!page.hasMore || page.nextCursorAt == null || page.nextCursorId == null) break;
    afterMatchDate = page.nextCursorAt;
    afterId = page.nextCursorId;
  }

  return urls;
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const staticEntries: MetadataRoute.Sitemap = [
    entry("/", undefined, "hourly", 1),
    entry("/picks", undefined, "hourly", 0.9),
    entry("/about", undefined, "monthly", 0.9),
    entry("/mcp", undefined, "monthly", 0.8),
    entry("/agent", undefined, "hourly", 0.8),
    entry("/disclaimer", undefined, "yearly", 0.3),
    entry("/privacy", undefined, "yearly", 0.3),
    entry("/terms", undefined, "yearly", 0.3),
    entry("/llms.txt", undefined, "monthly", 0.4),
    entry("/mcp.md", undefined, "monthly", 0.4),
  ];

  try {
    const [clubs, leagues, researched, finished] = await Promise.all([
      getClubs(),
      getLeagues(),
      getUpcomingResearchedMatches(),
      finishedMatchUrls(),
    ]);

    const clubEntries = clubs
      .filter((club) => club.slug)
      .map((club) => entry(clubPath(club.slug), undefined, "daily", 0.5));

    const leagueEntries = leagues
      .filter((league) => league.slug)
      .map((league) => entry(leaguePath(league.slug), undefined, "daily", 0.6));

    const researchedEntries = researched.map((match) =>
      entry(matchPath(match), new Date(match.matchDate), "hourly", 0.8),
    );

    return [...staticEntries, ...leagueEntries, ...clubEntries, ...researchedEntries, ...finished];
  } catch {
    return staticEntries;
  }
}
