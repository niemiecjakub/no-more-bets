import type { Metadata } from "next";
import { McpPageContent } from "./_components/mcp-page-content";
import { getMcpToolGroups } from "@/features/mcp/services/mcp-server";
import { JsonLd } from "@/components/json-ld";
import { softwareApplicationNode } from "@/lib/schema";
import { absoluteUrl } from "@/lib/site";

export const revalidate = 3600;

export const metadata: Metadata = {
  title: "Football MCP server for AI agents",
  description:
    "Fixtures, research, lineups, odds, clubs, standings over Model Context Protocol. Access on request.",
  alternates: { canonical: "/mcp" },
  openGraph: {
    title: "Football MCP server for AI agents",
    description:
      "Fixtures, research, lineups, odds, clubs, standings over Model Context Protocol.",
    url: "/mcp",
  },
};

export default async function McpPage() {
  let toolGroups: Awaited<ReturnType<typeof getMcpToolGroups>> = [];
  let error: string | null = null;
  try {
    toolGroups = await getMcpToolGroups();
  } catch {
    error = "Could not load MCP tools.";
  }

  return (
    <>
      <JsonLd
        data={{
          "@context": "https://schema.org",
          "@graph": [softwareApplicationNode(absoluteUrl("/mcp"))],
        }}
      />
      <McpPageContent toolGroups={toolGroups} error={error} />
    </>
  );
}
