import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { MatchPageClient } from "./match-page-client";
import { JsonLd } from "@/components/json-ld";
import { MATCH_STATUS } from "@/features/matches/interfaces";
import {
  getMatchAgentResearch,
  getMatchAnalysisPage,
  getMatchResearchBetSlip,
  isIndexableMatch,
} from "@/features/matches/services/matches-server";
import { breadcrumbList } from "@/lib/schema";
import { isBareNumericParam, leaguePath, matchPath, matchSlug, parseMatchParam } from "@/lib/paths";
import { absoluteUrl } from "@/lib/site";
import { formatMatchDay } from "@/utils/format-date";

export const revalidate = 120;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ id: string }>;
}): Promise<Metadata> {
  const { id: raw } = await params;
  const matchId = parseMatchParam(raw);
  if (matchId == null) return { title: "Match not found", robots: { index: false, follow: true } };

  const match = await getMatchAnalysisPage(matchId);
  if (!match) return { title: "Match not found", robots: { index: false, follow: true } };

  const research = await getMatchAgentResearch(matchId);
  const indexable = isIndexableMatch({
    hasResearch: research != null,
    matchStatusId: match.matchStatusId,
    homeGoals: match.homeGoals,
    awayGoals: match.awayGoals,
  });
  const slugInput = {
    id: match.matchId,
    homeClubSlug: match.homeClubSlug ?? "",
    awayClubSlug: match.awayClubSlug ?? "",
    matchDate: match.matchDate,
  };
  const path = matchPath(slugInput);
  const title = `${match.homeClubName} vs ${match.awayClubName} — AI research (${match.leagueName || "Football"}, ${formatMatchDay(match.matchDate)})`;
  const overview = research?.matchOverview.replace(/\s+/g, " ").trim() ?? "";
  const description = `${overview.slice(0, 140)}${overview.length > 140 ? "…" : ""}${overview ? " " : ""}Not betting advice.`.slice(0, 160);

    return {
    title,
    description,
    alternates: { canonical: path },
    robots: indexable ? { index: true, follow: true } : { index: false, follow: true },
    openGraph: {
      title,
      description,
      url: path,
      type: "article",
    },
  };
}

export default async function MatchPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: raw } = await params;
  const matchId = parseMatchParam(raw);
  if (matchId == null) notFound();

  const match = await getMatchAnalysisPage(matchId);
  if (!match) notFound();

  const slugInput = {
    id: match.matchId,
    homeClubSlug: match.homeClubSlug ?? "",
    awayClubSlug: match.awayClubSlug ?? "",
    matchDate: match.matchDate,
  };
  const canonicalSlug = matchSlug(slugInput);
  if (isBareNumericParam(raw) || raw !== canonicalSlug) {
    redirect(matchPath(slugInput));
  }

  const [research, researchSlip] = await Promise.all([
    getMatchAgentResearch(matchId),
    getMatchResearchBetSlip(matchId),
  ]);

  const sportsEvent = {
    "@type": "SportsEvent",
    name: `${match.homeClubName} vs ${match.awayClubName}`,
    startDate: match.matchDate,
    sport: "Football",
    competitor: [
      { "@type": "SportsTeam", name: match.homeClubName },
      { "@type": "SportsTeam", name: match.awayClubName },
    ],
    organizer: match.leagueName
      ? { "@type": "SportsOrganization", name: match.leagueName }
      : undefined,
    url: absoluteUrl(matchPath(slugInput)),
  };

  const article = research
    ? {
        "@type": "AnalysisNewsArticle",
        headline: `${match.homeClubName} vs ${match.awayClubName} — AI research`,
        dateModified: match.matchDate,
        author: [
          { "@type": "Organization", name: "No More Bets" },
          { "@type": "Person", name: "Chandler", description: "Autonomous research agent" },
        ],
        articleBody: research.matchOverview,
      }
    : null;

  const crumbs = [
    { name: "Home", path: "/" },
    ...(match.leagueSlug
      ? [{ name: match.leagueName || "League", path: leaguePath(match.leagueSlug) }]
      : []),
    { name: `${match.homeClubName} vs ${match.awayClubName}`, path: matchPath(slugInput) },
  ];

    return (
    <>
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [sportsEvent, breadcrumbList(crumbs), ...(article ? [article] : [])],
        }}
      />
      <MatchPageClient
        initialMatch={match}
        initialResearch={research}
        initialResearchSlip={researchSlip}
      />
    </>
    );
}
