import type { Metadata } from "next";
import { KnowledgeChat } from "@/features/knowledge/components/knowledge-chat";

export const metadata: Metadata = {
  description: "Chat with the agent's knowledge base.",
};

export default function KnowledgePage() {
  return <KnowledgeChat />;
}
