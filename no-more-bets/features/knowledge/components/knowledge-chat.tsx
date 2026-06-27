"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Loader2, Send } from "lucide-react";
import { cn } from "@/lib/utils";
import { Textarea } from "@/components/ui/textarea";
import type { KnowledgeMessage } from "../interfaces";
import {
  createKnowledgeMessage,
  delayMockReply,
  getMockKnowledgeReply,
  getMockReplyDelayMs,
} from "../_lib/mock-knowledge-chat";
import { KnowledgeMessageBubble } from "./knowledge-message-bubble";

const composerBoxClassName =
  "flex items-end gap-1.5 rounded-xl border border-zinc-200 bg-zinc-50 p-2 transition-colors focus-within:border-zinc-400 focus-within:ring-2 focus-within:ring-zinc-200/80 dark:border-zinc-800 dark:bg-zinc-900/50 dark:focus-within:border-zinc-600 dark:focus-within:ring-zinc-800";

const composerFieldClassName =
  "h-auto min-h-0 w-full rounded-none border-0 bg-transparent px-2 py-1.5 text-sm text-foreground shadow-none outline-none transition-colors placeholder:text-zinc-400 focus-visible:border-0 focus-visible:ring-0 disabled:bg-transparent disabled:opacity-50 dark:bg-transparent dark:placeholder:text-zinc-500 dark:focus-visible:ring-0 dark:disabled:bg-transparent [scrollbar-width:thin] [scrollbar-color:var(--color-zinc-400)_transparent] dark:[scrollbar-color:var(--color-zinc-600)_transparent] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-track]:bg-transparent [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-zinc-300 [&::-webkit-scrollbar-thumb]:hover:bg-zinc-400 dark:[&::-webkit-scrollbar-thumb]:bg-zinc-700 dark:[&::-webkit-scrollbar-thumb]:hover:bg-zinc-600";

const sendButtonClassName =
  "inline-flex size-9 shrink-0 items-center justify-center rounded-lg bg-zinc-900 text-white transition-colors hover:bg-zinc-800 disabled:pointer-events-none disabled:opacity-40 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200";

interface KnowledgeChatComposerProps {
  draft: string;
  isTyping: boolean;
  canSend: boolean;
  onDraftChange: (value: string) => void;
  onKeyDown: (event: React.KeyboardEvent<HTMLTextAreaElement>) => void;
  onSend: () => void;
}

function KnowledgeChatComposer({
  draft,
  isTyping,
  canSend,
  onDraftChange,
  onKeyDown,
  onSend,
}: KnowledgeChatComposerProps) {
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const adjustTextareaHeight = useCallback((textarea = textareaRef.current) => {
    if (!textarea) {
      return;
    }

    textarea.style.height = "auto";
    const maxHeight = 240;
    textarea.style.height = `${Math.min(textarea.scrollHeight, maxHeight)}px`;
    textarea.style.overflowY = textarea.scrollHeight > maxHeight ? "auto" : "hidden";
  }, []);

  useEffect(() => {
    adjustTextareaHeight();
  }, [draft, adjustTextareaHeight]);

  return (
    <div className="w-full px-4 py-3 sm:px-5">
      <div className={composerBoxClassName}>
        <Textarea
          ref={textareaRef}
          value={draft}
          onChange={(event) => {
            onDraftChange(event.target.value);
            adjustTextareaHeight(event.target);
          }}
          onKeyDown={onKeyDown}
          placeholder="Ask a question…"
          rows={1}
          disabled={isTyping}
          aria-label="Message"
          className={cn(
            composerFieldClassName,
            "min-h-9 max-h-60 resize-none overflow-y-auto leading-6",
          )}
        />
        <button
          type="button"
          onClick={onSend}
          disabled={!canSend}
          className={sendButtonClassName}
          aria-label="Send message"
        >
          {isTyping ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <Send className="size-4 shrink-0" aria-hidden />
          )}
        </button>
      </div>
    </div>
  );
}

export function KnowledgeChat() {
  const [messages, setMessages] = useState<KnowledgeMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const scrollAnchorRef = useRef<HTMLDivElement>(null);

  const hasMessages = messages.length > 0 || isTyping;

  const scrollToBottom = useCallback(() => {
    scrollAnchorRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, []);

  useEffect(() => {
    if (!hasMessages) {
      return;
    }
    scrollToBottom();
  }, [messages, isTyping, hasMessages, scrollToBottom]);

  const sendMessage = useCallback(async () => {
    const trimmed = draft.trim();
    if (trimmed.length === 0 || isTyping) {
      return;
    }

    const userMessage = createKnowledgeMessage("user", trimmed);
    setMessages((current) => [...current, userMessage]);
    setDraft("");
    setIsTyping(true);

    await delayMockReply(getMockReplyDelayMs());

    const assistantMessage = createKnowledgeMessage("assistant", getMockKnowledgeReply(trimmed));
    setMessages((current) => [...current, assistantMessage]);
    setIsTyping(false);
  }, [draft, isTyping]);

  function handleKeyDown(event: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key !== "Enter" || event.shiftKey) {
      return;
    }
    event.preventDefault();
    void sendMessage();
  }

  const canSend = draft.trim().length > 0 && !isTyping;

  return (
    <main className="mx-auto flex h-full min-h-0 w-full max-w-3xl flex-1 flex-col px-4 pt-6 sm:px-6">
      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-lg bg-white dark:bg-zinc-950">
        <header className="border-b border-zinc-200 px-4 py-4 dark:border-zinc-800 sm:px-5">
          <h1 className="text-lg font-semibold text-foreground">Knowledge</h1>
          <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            Ask about strategy, bankroll, and research — mocked for now.
          </p>
        </header>

        <div className="flex min-h-0 flex-1 flex-col">
          {hasMessages ? (
            <>
              <ul
                className="flex flex-1 flex-col gap-3 overflow-y-auto px-4 py-4 sm:px-5"
                aria-live="polite"
                aria-relevant="additions"
              >
                {messages.map((message) => (
                  <KnowledgeMessageBubble key={message.id} message={message} />
                ))}
                {isTyping ? (
                  <li className="flex justify-start" aria-label="Assistant is typing">
                    <div className="rounded-2xl rounded-bl-md bg-zinc-100 px-4 py-2.5 text-sm text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400">
                      <span className="inline-flex items-center gap-2">
                        <span className="inline-flex gap-1">
                          <span className="size-1.5 animate-pulse rounded-full bg-zinc-400 dark:bg-zinc-500" />
                          <span className="size-1.5 animate-pulse rounded-full bg-zinc-400 [animation-delay:150ms] dark:bg-zinc-500" />
                          <span className="size-1.5 animate-pulse rounded-full bg-zinc-400 [animation-delay:300ms] dark:bg-zinc-500" />
                        </span>
                        Assistant is typing…
                      </span>
                    </div>
                  </li>
                ) : null}
                <div ref={scrollAnchorRef} aria-hidden />
              </ul>

              <KnowledgeChatComposer
                draft={draft}
                isTyping={isTyping}
                canSend={canSend}
                onDraftChange={setDraft}
                onKeyDown={handleKeyDown}
                onSend={() => void sendMessage()}
              />
            </>
          ) : (
            <div className="flex flex-1 items-center justify-center px-4 sm:px-5">
              <KnowledgeChatComposer
                draft={draft}
                isTyping={isTyping}
                canSend={canSend}
                onDraftChange={setDraft}
                onKeyDown={handleKeyDown}
                onSend={() => void sendMessage()}
              />
            </div>
          )}
        </div>
      </div>
    </main>
  );
}
