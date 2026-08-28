import { getLeagues } from "@/features/leagues/services/leagues-server";
import { ABOUT_DEFINITION } from "@/lib/content-seo";
import { leaguePath } from "@/lib/paths";
import { getSiteUrl } from "@/lib/site";

export const revalidate = 3600;

const FALLBACK_LEAGUES = [
  { name: "Premier League", slug: "premier-league" },
  { name: "LaLiga", slug: "laliga" },
  { name: "Serie A", slug: "serie-a" },
  { name: "Bundesliga", slug: "bundesliga" },
  { name: "Ligue 1", slug: "ligue-1" },
  { name: "Ekstraklasa", slug: "ekstraklasa" },
  { name: "FIFA World Cup", slug: "fifa-world-cup" },
] as const;

function mdLink(title: string, url: string, note?: string) {
  return note ? `- [${title}](${url}): ${note}` : `- [${title}](${url})`;
}

export async function GET() {
  const site = getSiteUrl();
  let leagues: { name: string; slug: string }[] = [...FALLBACK_LEAGUES];

  try {
    const live = (await getLeagues()).filter(
      (league) =>
        league.slug &&
        league.slug !== "unknown" &&
        league.name.trim() !== "" &&
        league.name.trim().toLowerCase() !== "unknown",
    );
    if (live.length > 0) leagues = live;
  } catch {
    // keep fallback
  }

  const leagueLinks = leagues
    .map((league) =>
      mdLink(
        league.name,
        `${site}${leaguePath(league.slug)}`,
        `Upcoming researched fixtures and table`,
      ),
    )
    .join("\n");

  const body = `# No More Bets

> ${ABOUT_DEFINITION}

No More Bets is not the 2023 film of the same name. It is for people and AI clients who want inspectable football research, not tips. Published briefs, slips, sessions, and bankroll figures are a paper trail of one agent (Chandler). They are not betting or financial advice, and past outcomes do not predict future results.

Coverage: Premier League, LaLiga, Serie A, Bundesliga, Ligue 1, Ekstraklasa, FIFA World Cup.

URL patterns:
- Matches: ${site}/match/{home-slug}-vs-{away-slug}-{YYYY-MM-DD}-{id}
- Clubs: ${site}/club/{slug}
- Leagues: ${site}/leagues/{slug}

MCP access is granted on request. There is no public price list. Open a GitHub issue or use Feedback in the app; credentials are issued per request.

Citation and search crawlers (GPTBot, ChatGPT-User, PerplexityBot, ClaudeBot, anthropic-ai, Google-Extended) are allowed. Training-only scrapers (CCBot, Bytespider, Amazonbot, Applebot-Extended) are disallowed.

## Pages

${mdLink("About", `${site}/about`, "What the project is, methods, data sources")}
${mdLink("Matches", `${site}/`, "Upcoming fixtures and research status")}
${mdLink("Picks", `${site}/picks`, "Daily house slips by date")}
${mdLink("Agent", `${site}/agent`, "Public bankroll, pending slips, and session logs")}
${mdLink("MCP", `${site}/mcp`, "Football Model Context Protocol tools for AI clients")}
${mdLink("MCP catalog (markdown)", `${site}/mcp.md`, "Tool list, access, and example prompts")}
${mdLink("Disclaimer", `${site}/disclaimer`, "Not betting advice; how the daily loop works")}

## Leagues

${leagueLinks}

## Optional

${mdLink("Privacy", `${site}/privacy`)}
${mdLink("Terms", `${site}/terms`)}
${mdLink("Sitemap", `${site}/sitemap.xml`, "Full URL inventory including matches and clubs")}
${mdLink("robots.txt", `${site}/robots.txt`)}
${mdLink("GitHub", "https://github.com/niemiecjakub/no-more-bets")}
${mdLink("X", "https://x.com/nomorebetsai")}
${mdLink("BeGambleAware", "https://www.begambleaware.org/")}
`;

  return new Response(body, {
    headers: {
      "Content-Type": "text/plain; charset=utf-8",
      "Cache-Control": "public, max-age=3600, stale-while-revalidate=86400",
    },
  });
}
