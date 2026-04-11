"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { AgentSessionMessage } from "../services/agent-session-api";

const MESSAGE_MARKDOWN_CLASS =
  "text-sm text-foreground leading-6 [&_p]:my-2 [&_p:first-of-type]:mt-0 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700 wrap-break-word";

/** Matches `AgentSessionMessageKind.FunctionCall` on the API. */
const FUNCTION_CALL_KIND = 3;

export function agentSessionMessageKindLabel(kind: number): string {
  switch (kind) {
    case 1:
      return "Message";
    case 2:
      return "Reasoning";
    case 3:
      return "Function call";
    default:
      return `Kind ${kind}`;
  }
}

interface AgentSessionTranscriptProps {
  messages: AgentSessionMessage[];
}

export function AgentSessionTranscript({ messages }: AgentSessionTranscriptProps) {
  const visible = messages.filter((m) => m.kind !== FUNCTION_CALL_KIND);

  if (visible.length === 0) {
    return (
      <p className="px-3 py-3 text-sm text-zinc-500 dark:text-zinc-400">
        {messages.length === 0
          ? "No messages in this session."
          : "No transcript text to show (only tool calls were recorded)."}
      </p>
    );
  }
  return (
    <ul className="flex w-full min-w-0 flex-col divide-y divide-zinc-200 overflow-hidden border-0 bg-zinc-50/80 dark:divide-zinc-800 dark:bg-zinc-900/40">
      {visible.map((m) => (
        <li key={m.id} className="px-3 py-3 text-sm">
          <div className="mb-0 flex flex-wrap items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400">
            <span className="rounded bg-zinc-200 px-1.5 py-0.5 font-medium text-zinc-800 dark:bg-zinc-800 dark:text-zinc-200">
              {agentSessionMessageKindLabel(m.kind)}
            </span>
          </div>
          <div className={MESSAGE_MARKDOWN_CLASS}>
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{m.text}</ReactMarkdown>
          </div>
        </li>
      ))}
    </ul>
  );
}
