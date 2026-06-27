export interface KnowledgeMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: Date;
}
