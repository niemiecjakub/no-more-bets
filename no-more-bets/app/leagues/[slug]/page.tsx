import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Breadcrumbs } from "@/components/breadcrumbs";
import { JsonLd } from "@/components/json-ld";
import { ClubLeagueTable } from "@/features/clubs/components/club-league-table";
import { ClubWorldCupGroupTables } from "@/features/clubs/components/club-world-cup-group-tables";
import { MatchList } from "@/features/matches/components/match-list";
import { MATCH_STATUS } from "@/features/matches/interfaces";
import { MATCH_DATE_SORT } from "@/features/matches/services/matches-api";
import { getMatchesPage } from "@/features/matches/services/matches-server";
import { getClubs } from "@/features/clubs/services/clubs-server";
import { getLeagueTable, getLeagues } from "@/features/leagues/services/leagues-server";
import { breadcrumbList } from "@/lib/schema";
import { leaguePath, matchPath } from "@/lib/paths";
import { absoluteUrl } from "@/lib/site";

export const revalidate = 120;

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const leagues = await getLeagues().catch(() => []);
  const league = leagues.find((item) => item.slug === slug);
  if (!league) return { title: "League not found", robots: { index: false, follow: true } };
  const title = `${league.name} — AI match research`;
  const description = `Upcoming researched fixtures and table for ${league.name}.`;
  return {
    title,
    description,
    alternates: { canonical: leaguePath(league.slug) },
    openGraph: { title, description, url: leaguePath(league.slug) },
  };
}

export default async function LeaguePage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const leagues = await getLeagues().catch(() => []);
  const league = leagues.find((item) => item.slug === slug);
  if (!league) notFound();

  const clubs = await getClubs().catch(() => []);
  const membership = clubs
    .flatMap((club) => club.memberships.map((item) => ({ ...item, clubId: club.id })))
    .filter((item) => item.leagueId === league.id)
    .sort((a, b) => (b.startDate ?? "").localeCompare(a.startDate ?? ""))[0];

  const [table, upcoming] = await Promise.all([
    membership
      ? getLeagueTable(league.id, membership.seasonId)
      : Promise.resolve(null),
    getMatchesPage({
      matchStatusId: MATCH_STATUS.Upcoming,
      leagueIds: [league.id],
      sortOrder: MATCH_DATE_SORT.Ascending,
    }).catch(() => ({ items: [], hasMore: false, nextCursorAt: null, nextCursorId: null })),
  ]);

  const researched = upcoming.items.filter((match) => match.hasResearch);

  return (
    <main className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6">
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [
            breadcrumbList([
              { name: "Home", path: "/" },
              { name: league.name, path: leaguePath(league.slug) },
            ]),
            {
              "@type": "ItemList",
              name: `Upcoming researched ${league.name} matches`,
              itemListElement: researched.map((match, index) => ({
                "@type": "ListItem",
                position: index + 1,
                url: absoluteUrl(matchPath(match)),
                name: `${match.homeClubName} vs ${match.awayClubName}`,
              })),
            },
          ],
        }}
      />
      <Breadcrumbs
        items={[
          { name: "Home", href: "/" },
          { name: league.name },
        ]}
      />
      <h1 className="text-3xl font-semibold tracking-tight text-foreground">
        {league.name} — AI match research
      </h1>
      <p className="mt-2 mb-8 max-w-2xl text-sm leading-6 text-zinc-600 dark:text-zinc-300">
        Upcoming fixtures the agent can research, plus the current table. Empty stubs without a
        brief are still listed in the app.
      </p>

      <section className="mb-10">
        <h2 className="mb-4 text-xl font-semibold text-foreground">Upcoming researched matches</h2>
        {researched.length === 0 ? (
          <p className="text-sm text-zinc-500">No researched upcoming fixtures yet.</p>
        ) : (
          <MatchList matches={researched} />
        )}
      </section>

      <section>
        <h2 className="mb-4 text-xl font-semibold text-foreground">Table</h2>
        {table == null ? (
          <p className="text-sm text-zinc-500">No table snapshot available.</p>
        ) : table.groups && table.groups.length > 0 ? (
          <ClubWorldCupGroupTables table={table} highlightClubId={0} />
        ) : (
            <ClubLeagueTable table={table} highlightClubId={-1} />
        )}
      </section>
    </main>
  );
}
