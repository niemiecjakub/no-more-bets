"use client";

import { Lightbulb, MessageSquareText, Wrench } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { AgentSessionMessage } from "../services/agent-session-api";

const MESSAGE_MARKDOWN_CLASS =
  "text-sm text-foreground leading-6 [&_p]:my-2 [&_p:first-of-type]:mt-0 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700 wrap-break-word";

/** Matches `AgentSessionMessageKind.FunctionCall` on the API. */
const FUNCTION_CALL_KIND = 3;

interface MessageKindBadgeStyle {
  label: string;
  icon: LucideIcon;
  className: string;
}

function messageKindBadgeStyle(kind: number): MessageKindBadgeStyle {
  switch (kind) {
    case 1:
      return {
        label: "Message",
        icon: MessageSquareText,
        className:
          "border-blue-200 bg-blue-50 text-blue-700 dark:border-blue-700 dark:bg-blue-900/30 dark:text-blue-200",
      };
    case 2:
      return {
        label: "Reasoning",
        icon: Lightbulb,
        className:
          "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-200",
      };
    case 3:
      return {
        label: "Function call",
        icon: Wrench,
        className:
          "border-orange-200 bg-orange-50 text-orange-700 dark:border-orange-700 dark:bg-orange-900/30 dark:text-orange-200",
      };
    default:
      return {
        label: `Kind ${kind}`,
        icon: MessageSquareText,
        className:
          "border-zinc-300 bg-zinc-200 text-zinc-800 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-200",
      };
  }
}

interface AgentSessionTranscriptProps {
  messages: AgentSessionMessage[];
}

export function AgentSessionTranscript({ messages }: AgentSessionTranscriptProps) {
  const visible = messages.filter((m) => m.kind !== FUNCTION_CALL_KIND);

  if (visible.length === 0) {
    return (
      <p className="px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400">
        {messages.length === 0
          ? "No messages in this session."
          : "No transcript text to show (only tool calls were recorded)."}
      </p>
    );
  }
  return (
    <ul className="flex w-full min-w-0 flex-1 flex-col divide-y divide-zinc-200 overflow-hidden border-0 bg-zinc-50/80 dark:divide-zinc-800 dark:bg-zinc-900/40">
      {visible.map((m) => {
        const badge = messageKindBadgeStyle(m.kind);
        const BadgeIcon = badge.icon;

        return (
          <li key={m.id} className="w-full min-w-0 px-4 py-3 text-sm">
            <div className="mb-0 flex flex-wrap items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400">
              <span
                className={`inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 font-medium ${badge.className}`}
              >
                <BadgeIcon className="h-3 w-3" aria-hidden />
                {badge.label}
              </span>
            </div>
            <div className={MESSAGE_MARKDOWN_CLASS}>
              <ReactMarkdown remarkPlugins={[remarkGfm]}>{m.text}</ReactMarkdown>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
