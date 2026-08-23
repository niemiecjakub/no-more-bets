import { getMcpToolGroups } from "@/features/mcp/services/mcp-server";
import { getSiteUrl } from "@/lib/site";

export const revalidate = 3600;

export async function GET() {
  const site = getSiteUrl();
  let catalog = "_Tool list unavailable._";

  try {
    const groups = await getMcpToolGroups();
    catalog = groups
      .map((group) => {
        const tools = group.tools
          .map((tool) => `- **${tool.name}** (${tool.title}): ${tool.description}`)
          .join("\n");
        return `## ${group.label}\n\n${group.description}\n\n${tools}`;
      })
      .join("\n\n");
  } catch {
    // keep fallback
  }

  const body = `# Football MCP server — No More Bets

The No More Bets MCP server exposes the same structured football data the public agent uses: fixtures, research briefs, lineups, injuries, odds, clubs, and standings.

- Site: ${site}/mcp
- Access: on request — open a GitHub issue at https://github.com/niemiecjakub/no-more-bets/issues or use Feedback in the app
- Pricing: none published; access is granted for research and agent tooling
- Auth: credentials issued per request (not a public anonymous endpoint)

## Example prompts

- Upcoming researched Premier League fixtures this weekend
- Agent research for a named home vs away match
- Club next match, recent form, and table position
- Current 1X2 odds and lineup availability

${catalog}

This is not betting advice. Tool output is data and stored research text.
`;

  return new Response(body, {
    headers: {
      "Content-Type": "text/markdown; charset=utf-8",
      "Cache-Control": "public, max-age=3600, stale-while-revalidate=86400",
    },
  });
}
