import type { AgentSessionMessage } from "../services/agent-session-api";

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

export interface FunctionCallArgument {
  name: string;
  value?: string | null;
}

export interface FunctionCallPayload {
  name: string;
  arguments?: FunctionCallArgument[] | null;
}

export interface SimulatedTodoItem {
  id: number;
  title: string;
  isComplete: boolean;
}

export interface SimulatedTodoState {
  items: SimulatedTodoItem[];
  nextId: number;
}

export interface TodoCompleteItem {
  id: number;
  reason: string | null;
}

function parseJsonValue(value: string | null | undefined): unknown {
  if (value == null || value === "") return null;
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function findArgumentValue(payload: FunctionCallPayload, argumentName: string): unknown {
  const arg = payload.arguments?.find((a) => a.name === argumentName);
  return parseJsonValue(arg?.value);
}

function asArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (value == null) return [];
  return [value];
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
        });
      }
      break;
    }
    case "todos_complete": {
      const idSet = new Set(extractCompleteItems(payload).map((item) => item.id));
      for (const item of state.items) {
        if (!item.isComplete && idSet.has(item.id)) {
          item.isComplete = true;
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

export function parseFunctionCallText(text: string): FunctionCallPayload | null {
  try {
    const parsed: unknown = JSON.parse(text);
    if (parsed != null && typeof parsed === "object" && "name" in parsed) {
      const name = (parsed as { name: unknown }).name;
      if (typeof name === "string") {
        return parsed as FunctionCallPayload;
      }
    }
  } catch {
    // ignore invalid JSON
  }
  return null;
}

export function isTodoToolCall(message: AgentSessionMessage): boolean {
  if (message.kind !== FUNCTION_CALL_KIND) return false;
  const payload = parseFunctionCallText(message.text);
  return payload != null && TODO_TOOL_NAMES.has(payload.name);
}
