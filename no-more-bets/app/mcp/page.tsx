import type { Metadata } from "next";
import { McpPageContent } from "./_components/mcp-page-content";

export const metadata: Metadata = {
  description:
    "Model Context Protocol tools for football matches, clubs, and leagues — research fixtures, odds, lineups, and standings from your AI client.",
};

export default function McpPage() {
  return <McpPageContent />;
}
