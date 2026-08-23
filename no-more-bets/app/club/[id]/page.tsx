import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { ClubPageClient } from "./club-page-client";
import { JsonLd } from "@/components/json-ld";
import { getClubById, getClubBySlug, getClubNextMatch, getClubRecentGames } from "@/features/clubs/services/clubs-server";
import { getLeagueTable } from "@/features/leagues/services/leagues-server";
import { resolveDefaultMembership } from "@/features/clubs/resolve-default-membership";
import { breadcrumbList } from "@/lib/schema";
import { clubPath, isBareNumericParam } from "@/lib/paths";
import { absoluteUrl } from "@/lib/site";

export const revalidate = 120;

export async function generateMetadata({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ tab?: string; seasonId?: string }>;
}): Promise<Metadata> {
  const { id: raw } = await params;
  const sp = await searchParams;
  const club = isBareNumericParam(raw)
    ? await getClubById(Number(raw))
    : await getClubBySlug(raw);
  if (!club) return { title: "Club not found", robots: { index: false, follow: true } };

  const parameterized = Boolean(sp.tab || sp.seasonId);
  const title = `${club.name} — matches and AI betting record`;
  const description = `Next fixture, recent form, and agent selection stats for ${club.name} on No More Bets.`;
  return {
    title,
    description,
    alternates: { canonical: clubPath(club.slug) },
    robots: parameterized ? { index: false, follow: true } : { index: true, follow: true },
    openGraph: { title, description, url: clubPath(club.slug) },
  };
}

export default async function ClubPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id: raw } = await params;
  const club = isBareNumericParam(raw)
    ? await getClubById(Number(raw))
    : await getClubBySlug(raw);
  if (!club) notFound();
  if (isBareNumericParam(raw) || raw !== club.slug) {
    redirect(clubPath(club.slug));
  }

  const [nextMatch, recentGames] = await Promise.all([
    getClubNextMatch(club.id),
    getClubRecentGames(club.id),
  ]);

  const membership = resolveDefaultMembership(club.memberships);
  const table = membership
    ? await getLeagueTable(
        membership.leagueId,
        membership.seasonId,
        membership.leagueSlug === "fifa-world-cup" ? club.id : undefined,
      )
    : null;

  return (
    <>
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [
            {
              "@type": "SportsTeam",
              name: club.name,
              url: absoluteUrl(clubPath(club.slug)),
              sport: "Football",
            },
            breadcrumbList([
              { name: "Home", path: "/" },
              { name: club.name, path: clubPath(club.slug) },
            ]),
          ],
        }}
      />
      <ClubPageClient
        initialClub={club}
        initialNextMatch={nextMatch}
        initialRecentGames={recentGames}
        initialTable={table}
      />
    </>
  );
}
