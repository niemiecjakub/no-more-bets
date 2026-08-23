import type { Metadata } from "next";
import { AgentPageClient } from "./agent-page-client";
import { getAgentBankrollWidget } from "@/features/bets/services/agent-server";

export const revalidate = 30;

export async function generateMetadata({
  searchParams,
}: {
  searchParams: Promise<{ widget?: string; search?: string; sessionId?: string }>;
}): Promise<Metadata> {
  const sp = await searchParams;
  const parameterized = Boolean(sp.widget || sp.search || sp.sessionId);
  return {
    title: "Public bankroll and betting log",
    description:
      "Live bankroll, pending slips, sessions, and memories from the No More Bets football research agent.",
    alternates: { canonical: "/agent" },
    robots: parameterized ? { index: false, follow: true } : { index: true, follow: true },
    openGraph: {
      title: "Public bankroll and betting log",
      description: "Live bankroll, pending slips, sessions, and memories.",
      url: "/agent",
    },
  };
}

export default async function AgentPage() {
  let bankroll = null;
  try {
    bankroll = await getAgentBankrollWidget();
  } catch {
    bankroll = null;
  }

  return <AgentPageClient initialBankroll={bankroll} />;
}
