import type { AgentSessionMessage } from "../services/agent-session-api";
import {
  asArray,
  findArgumentValue,
  parseFunctionCallText,
  type FunctionCallPayload,
} from "./function-call";

export type { FunctionCallArgument, FunctionCallPayload } from "./function-call";
export { parseFunctionCallText } from "./function-call";

/** Matches tool names in TodoProvider.cs */
export const TODO_TOOL_NAMES = new Set([
  "todos_add",
  "todos_complete",
  "todos_remove",
  "todos_get_remaining",
  "todos_get_all",
]);

/** Matches `AgentSessionMessageKind.FunctionCall` on the API. */
const FUNCTION_CALL_KIND = 3;

export interface SimulatedTodoItem {
  id: number;
  title: string;
  isComplete: boolean;
  completionReason: string | null;
}

export interface SimulatedTodoState {
  items: SimulatedTodoItem[];
  nextId: number;
}

export interface TodoCompleteItem {
  id: number;
  reason: string | null;
}

export function createEmptyTodoState(): SimulatedTodoState {
  return { items: [], nextId: 1 };
}

export function cloneTodoState(state: SimulatedTodoState): SimulatedTodoState {
  return {
    nextId: state.nextId,
    items: state.items.map((item) => ({ ...item })),
  };
}

export function extractAddTitles(payload: FunctionCallPayload): string[] {
  const todos = asArray(findArgumentValue(payload, "todos"));
  return todos
    .map((item) => {
      if (item != null && typeof item === "object" && "title" in item) {
        const title = (item as { title?: unknown }).title;
        return typeof title === "string" ? title.trim() : null;
      }
      return typeof item === "string" ? item.trim() : null;
    })
    .filter((title): title is string => title != null && title.length > 0);
}

export function extractCompleteItems(payload: FunctionCallPayload): TodoCompleteItem[] {
  const items = asArray(findArgumentValue(payload, "items"));
  return items
    .map((item) => {
      if (item == null || typeof item !== "object") return null;
      const { id, reason } = item as { id?: unknown; reason?: unknown };
      const numericId = typeof id === "number" ? id : Number(id);
      if (!Number.isFinite(numericId)) return null;
      const reasonText = typeof reason === "string" && reason.trim().length > 0 ? reason.trim() : null;
      return { id: numericId, reason: reasonText };
    })
    .filter((item): item is TodoCompleteItem => item != null);
}

export function extractRemoveIds(payload: FunctionCallPayload): number[] {
  const idsValue = findArgumentValue(payload, "ids");
  return asArray(idsValue)
    .map((id) => (typeof id === "number" ? id : Number(id)))
    .filter((id) => Number.isFinite(id));
}

export function resolveTodoTitle(state: SimulatedTodoState, id: number): string | null {
  const item = state.items.find((todo) => todo.id === id);
  return item?.title ?? null;
}

export function resolveTodoItem(state: SimulatedTodoState, id: number): SimulatedTodoItem | null {
  return state.items.find((todo) => todo.id === id) ?? null;
}

export function applyTodoAction(state: SimulatedTodoState, payload: FunctionCallPayload): void {
  switch (payload.name) {
    case "todos_add": {
      for (const title of extractAddTitles(payload)) {
        state.items.push({
          id: state.nextId++,
          title,
          isComplete: false,
          completionReason: null,
        });
      }
      break;
    }
    case "todos_complete": {
      for (const { id, reason } of extractCompleteItems(payload)) {
        const item = state.items.find((todo) => todo.id === id);
        if (item != null && !item.isComplete) {
          item.isComplete = true;
          item.completionReason = reason;
        }
      }
      break;
    }
    case "todos_remove": {
      const idSet = new Set(extractRemoveIds(payload));
      state.items = state.items.filter((item) => !idSet.has(item.id));
      break;
    }
    case "todos_get_remaining":
    case "todos_get_all":
      break;
  }
}

export function isTodoToolCall(message: AgentSessionMessage): boolean {
  if (message.kind !== FUNCTION_CALL_KIND) return false;
  const payload = parseFunctionCallText(message.text);
  return payload != null && TODO_TOOL_NAMES.has(payload.name);
}
