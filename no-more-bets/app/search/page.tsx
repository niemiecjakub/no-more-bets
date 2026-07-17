import type { Metadata } from "next";
import { Suspense } from "react";
import { SemanticSearchPage } from "@/features/matches/components/semantic-search-page";

export const metadata: Metadata = {
  description: "Semantic search across match and research content.",
};

export default function SearchPage() {
  return (
    <Suspense fallback={null}>
      <SemanticSearchPage />
    </Suspense>
  );
}
