import type { Metadata } from "next";
import { fetchClubById } from "@/features/clubs/services/club-detail-api";

type LayoutProps = {
  children: React.ReactNode;
  params: Promise<{ id: string }>;
};

function parseClubId(id: string | undefined): number | null {
  if (id == null || id === "") return null;
  const clubId = Number(id);
  if (!Number.isFinite(clubId) || clubId < 1) return null;
  return clubId;
}

export async function generateMetadata({ params }: Pick<LayoutProps, "params">): Promise<Metadata> {
  const { id } = await params;
  const clubId = parseClubId(id);
  if (clubId == null) {
    return { title: "Club" };
  }

  try {
    const club = await fetchClubById(clubId);
    return { title: club.name };
  } catch {
    return { title: "Club" };
  }
}

export default function ClubLayout({ children }: Pick<LayoutProps, "children">) {
  return children;
}
