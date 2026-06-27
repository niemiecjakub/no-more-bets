import type { KnowledgeMessage } from "../interfaces";

const MOCK_REPLY_DELAY_MS_MIN = 600;
const MOCK_REPLY_DELAY_MS_MAX = 900;

function createMessageId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

export function createKnowledgeMessage(
  role: KnowledgeMessage["role"],
  content: string,
  createdAt: Date = new Date(),
): KnowledgeMessage {
  return {
    id: createMessageId(),
    role,
    content,
    createdAt,
  };
}

export const SEED_KNOWLEDGE_MESSAGES: KnowledgeMessage[] = [
  createKnowledgeMessage(
    "assistant",
    "Welcome to the knowledge base. This is where the agent's durable learnings about strategy, bankroll, and research live. Ask me anything — responses are mocked for now.",
    new Date(Date.now() - 120_000),
  ),
  createKnowledgeMessage(
    "user",
    "How does the agent think about bankroll discipline?",
    new Date(Date.now() - 90_000),
  ),
  createKnowledgeMessage(
    "assistant",
    "The agent treats bankroll as a hard constraint, not a suggestion. Stake sizing scales with confidence and remaining runway, and it avoids chasing losses or over-betting thin edges.",
    new Date(Date.now() - 60_000),
  ),
];

const KEYWORD_REPLIES: { keywords: string[]; reply: string }[] = [
  {
    keywords: ["bankroll", "stake", "staking", "money"],
    reply:
      "Bankroll management is central: the agent sizes stakes relative to confidence and remaining runway, never risking more than the session rules allow.",
  },
  {
    keywords: ["strategy", "approach", "discipline"],
    reply:
      "Strategy memories capture repeatable patterns — when to pass on a fixture, how to weigh form vs. odds, and when an edge is too thin to bet.",
  },
  {
    keywords: ["memory", "memories", "remember", "recall"],
    reply:
      "Memories are categorized (strategy, bankroll, reflections, general knowledge). After each session the agent distills what is worth keeping for future research.",
  },
  {
    keywords: ["bet", "bets", "wager", "odds"],
    reply:
      "Bets are placed only after structured research. The agent documents reasoning in session transcripts and links outcomes back to its memory for reflection.",
  },
  {
    keywords: ["research", "fixture", "match"],
    reply:
      "Research builds a structured view of each fixture — form, injuries, context — before any decision. Internet research and prior memories feed into that picture.",
  },
];

export function getMockKnowledgeReply(input: string): string {
  const normalized = input.toLowerCase();

  for (const { keywords, reply } of KEYWORD_REPLIES) {
    if (keywords.some((keyword) => normalized.includes(keyword))) {
      return reply;
    }
  }

  return "I'm mocked for now, but I can help with strategy, bankroll, and research topics. Try asking about one of those.";
}

export function getMockReplyDelayMs(): number {
  return (
    MOCK_REPLY_DELAY_MS_MIN +
    Math.floor(Math.random() * (MOCK_REPLY_DELAY_MS_MAX - MOCK_REPLY_DELAY_MS_MIN + 1))
  );
}

export function delayMockReply(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}
