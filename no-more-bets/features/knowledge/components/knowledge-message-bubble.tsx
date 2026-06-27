import { cn } from "@/lib/utils";
import type { KnowledgeMessage } from "../interfaces";

interface KnowledgeMessageBubbleProps {
  message: KnowledgeMessage;
}

export function KnowledgeMessageBubble({ message }: KnowledgeMessageBubbleProps) {
  const isUser = message.role === "user";

  return (
    <li
      className={cn("flex", isUser ? "justify-end" : "justify-start")}
      aria-label={isUser ? "Your message" : "Assistant message"}
    >
      <div
        className={cn(
          "max-w-[85%] text-sm leading-6 wrap-break-word",
          isUser
            ? "rounded-2xl rounded-br-md bg-zinc-100 px-4 py-2.5 text-foreground dark:bg-zinc-800"
            : "text-foreground dark:text-zinc-50",
        )}
      >
        {message.content}
      </div>
    </li>
  );
}
