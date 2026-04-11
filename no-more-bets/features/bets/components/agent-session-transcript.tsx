"use client";

import type { AgentSessionMessage } from "../services/agent-session-api";

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
  if (messages.length === 0) {
    return (
      <p className="px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400">
        No messages in this session.
      </p>
    );
  }
  return (
    <ul className="flex flex-col gap-3 px-4 py-3">
      {messages.map((m) => (
        <li
          key={m.id}
          className="rounded-md border border-zinc-200 bg-zinc-50/80 p-3 text-sm dark:border-zinc-800 dark:bg-zinc-900/40"
        >
          <div className="mb-1 flex flex-wrap items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400">
            <span className="font-medium text-zinc-600 dark:text-zinc-300">#{m.ordinal}</span>
            <span className="rounded bg-zinc-200 px-1.5 py-0.5 font-medium text-zinc-800 dark:bg-zinc-800 dark:text-zinc-200">
              {agentSessionMessageKindLabel(m.kind)}
            </span>
          </div>
          <pre className="whitespace-pre-wrap break-words font-sans text-sm leading-6 text-zinc-800 dark:text-zinc-200">
            {m.text}
          </pre>
        </li>
      ))}
    </ul>
  );
}
