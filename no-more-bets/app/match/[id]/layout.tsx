import type { Metadata } from "next";
import { fetchMatchAnalysisPage } from "@/features/matches/services/matches-api";

type LayoutProps = {
  children: React.ReactNode;
  params: Promise<{ id: string }>;
};

function parseMatchId(id: string | undefined): number | null {
  if (id == null || id === "") return null;
  const matchId = Number(id);
  if (!Number.isFinite(matchId) || matchId < 1) return null;
  return matchId;
}

export async function generateMetadata({ params }: Pick<LayoutProps, "params">): Promise<Metadata> {
  const { id } = await params;
  const matchId = parseMatchId(id);
  if (matchId == null) {
    return { title: "Match" };
  }

  try {
    const data = await fetchMatchAnalysisPage(matchId);
    return {
      title: `${data.homeClubName} vs ${data.awayClubName}`,
    };
  } catch {
    return { title: "Match" };
  }
}

export default function MatchLayout({ children }: Pick<LayoutProps, "children">) {
  return children;
}
