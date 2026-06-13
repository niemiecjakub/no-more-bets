"use client";

import { Lightbulb, MessageSquareText } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { MatchResearchOutputView } from "@/features/matches/components/match-research-output-view";
import { parseMatchResearchOutputText } from "@/features/matches/services/match-insights-api";
import { TodoToolCallView } from "./todo-tool-call-view";
import { getToolCallBadgeStyle, ToolCallView } from "./tool-call-view";
import type { AgentSessionMessage } from "../services/agent-session-api";
import {
  applyTodoAction,
  cloneTodoState,
  createEmptyTodoState,
  isTodoToolCall,
  parseFunctionCallText,
  type SimulatedTodoState,
} from "../utils/todo-tool-call";

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
  /** Hide the structured research JSON payload (shown separately on match pages). */
  hideStructuredResearchOutput?: boolean;
}

function buildTodoStateByMessageId(messages: AgentSessionMessage[]): Map<number, SimulatedTodoState> {
  const stateByMessageId = new Map<number, SimulatedTodoState>();
  const runningState = createEmptyTodoState();

  for (const message of messages) {
    if (!isTodoToolCall(message)) continue;
    const payload = parseFunctionCallText(message.text);
    if (payload == null) continue;

    stateByMessageId.set(message.id, cloneTodoState(runningState));
    applyTodoAction(runningState, payload);
  }

  return stateByMessageId;
}

function isFunctionCall(message: AgentSessionMessage): boolean {
  return message.kind === FUNCTION_CALL_KIND;
}

function fallbackToolCallBadge(message: AgentSessionMessage): MessageKindBadgeStyle {
  const payload = parseFunctionCallText(message.text);
  return {
    label: payload?.name ?? "Tool call",
    icon: MessageSquareText,
    className:
      "border-zinc-300 bg-zinc-100 text-zinc-800 dark:border-zinc-600 dark:bg-zinc-800 dark:text-zinc-200",
  };
}

export function AgentSessionTranscript({
  messages,
  hideStructuredResearchOutput = false,
}: AgentSessionTranscriptProps) {
  const visible = messages.filter(
    (m) => !(hideStructuredResearchOutput && parseMatchResearchOutputText(m.text) != null),
  );

  const todoStateByMessageId = buildTodoStateByMessageId(visible);

  if (visible.length === 0) {
    return (
      <p className="px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400">
        No messages in this session.
      </p>
    );
  }
  return (
    <ul className="flex w-full min-w-0 flex-1 flex-col divide-y divide-zinc-200 overflow-hidden border-0 bg-zinc-50/80 dark:divide-zinc-800 dark:bg-zinc-900/40">
      {visible.map((m) => {
        const isTodo = isTodoToolCall(m);
        const isToolCall = isFunctionCall(m);
        const functionPayload = isToolCall ? parseFunctionCallText(m.text) : null;
        const badge =
          isToolCall && m.toolCallDisplay != null
            ? getToolCallBadgeStyle(m.toolCallDisplay)
            : isToolCall
              ? fallbackToolCallBadge(m)
              : messageKindBadgeStyle(m.kind);
        const BadgeIcon = badge.icon;
        const structuredResearch = parseMatchResearchOutputText(m.text);
        const todoPayload = isTodo ? functionPayload : null;
        const todoState = todoPayload ? todoStateByMessageId.get(m.id) : undefined;

        return (
          <li key={m.id} className="w-full min-w-0 px-4 py-3 text-sm">
            <div className="mb-1.5 flex flex-wrap items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400">
              <span
                className={`inline-flex items-center gap-1 rounded-md border px-1.5 py-0.5 font-medium ${badge.className}`}
              >
                <BadgeIcon className="h-3 w-3" aria-hidden />
                {badge.label}
              </span>
            </div>
            {todoPayload && todoState ? (
              <TodoToolCallView payload={todoPayload} state={todoState} />
            ) : isToolCall && m.toolCallDisplay != null ? (
              <ToolCallView display={m.toolCallDisplay} />
            ) : structuredResearch ? (
              <MatchResearchOutputView research={structuredResearch} />
            ) : (
              <div className={MESSAGE_MARKDOWN_CLASS}>
                <ReactMarkdown remarkPlugins={[remarkGfm]}>{m.text}</ReactMarkdown>
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
