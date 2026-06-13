"use client";

import type { ReactNode } from "react";
import { Circle, CircleCheck, CircleMinus, type LucideIcon } from "lucide-react";
import type { FunctionCallPayload, SimulatedTodoState } from "../utils/todo-tool-call";
import {
  extractAddTitles,
  extractCompleteItems,
  extractRemoveIds,
  resolveTodoTitle,
} from "../utils/todo-tool-call";

const OPEN_TODO_ICON_CLASS = "text-amber-600 dark:text-amber-400";
const COMPLETED_TODO_ICON_CLASS =
  "fill-amber-100 text-amber-700 dark:fill-amber-900/50 dark:text-amber-300";
const COMPLETED_TODO_TITLE_CLASS = "text-zinc-500 line-through dark:text-zinc-400";

interface TodoToolCallViewProps {
  payload: FunctionCallPayload;
  state: SimulatedTodoState;
}

interface TodoListRowProps {
  icon: LucideIcon;
  iconClassName: string;
  title: string;
  titleClassName?: string;
  reason?: string | null;
}

function TodoListRow({ icon: Icon, iconClassName, title, titleClassName, reason }: TodoListRowProps) {
  return (
    <li className="flex gap-2 py-1">
      <Icon className={`mt-0.5 h-4 w-4 shrink-0 ${iconClassName}`} aria-hidden />
      <div className="min-w-0 flex-1">
        <p className={`text-sm leading-5 ${titleClassName ?? "text-foreground"}`}>{title}</p>
        {reason ? (
          <p className="mt-0.5 text-xs leading-5 text-zinc-500 dark:text-zinc-400">{reason}</p>
        ) : null}
      </div>
    </li>
  );
}

function TodoList({ children }: { children: ReactNode }) {
  return <ul className="flex flex-col">{children}</ul>;
}

function EmptyTodoAction({ message }: { message: string }) {
  return <p className="text-sm text-zinc-500 dark:text-zinc-400">{message}</p>;
}

function renderAdd(payload: FunctionCallPayload, state: SimulatedTodoState) {
  const titles = extractAddTitles(payload);
  if (titles.length === 0) {
    return <EmptyTodoAction message="No todo items added." />;
  }

  return (
    <TodoList>
      {titles.map((title, index) => (
        <TodoListRow
          key={`${state.nextId + index}-${title}`}
          icon={Circle}
          iconClassName={OPEN_TODO_ICON_CLASS}
          title={title}
        />
      ))}
    </TodoList>
  );
}

function renderComplete(payload: FunctionCallPayload, state: SimulatedTodoState) {
  const completedNow = new Map(
    extractCompleteItems(payload).map((item) => [item.id, item.reason] as const),
  );

  if (state.items.length === 0) {
    return <EmptyTodoAction message="No todo items completed." />;
  }

  return (
    <TodoList>
      {state.items.map((item) => {
        const justCompleted = completedNow.has(item.id);
        const isComplete = item.isComplete || justCompleted;

        return (
          <TodoListRow
            key={item.id}
            icon={isComplete ? CircleCheck : Circle}
            iconClassName={isComplete ? COMPLETED_TODO_ICON_CLASS : OPEN_TODO_ICON_CLASS}
            title={item.title}
            titleClassName={isComplete ? COMPLETED_TODO_TITLE_CLASS : undefined}
            reason={
              isComplete
                ? (justCompleted ? (completedNow.get(item.id) ?? null) : item.completionReason)
                : null
            }
          />
        );
      })}
    </TodoList>
  );
}

function renderRemove(payload: FunctionCallPayload, state: SimulatedTodoState) {
  const ids = extractRemoveIds(payload);
  if (ids.length === 0) {
    return <EmptyTodoAction message="No todo items removed." />;
  }

  return (
    <TodoList>
      {ids.map((id) => {
        const title = resolveTodoTitle(state, id) ?? `Todo #${id}`;
        return (
          <TodoListRow
            key={id}
            icon={CircleMinus}
            iconClassName="text-zinc-400 dark:text-zinc-500"
            title={title}
            titleClassName={COMPLETED_TODO_TITLE_CLASS}
          />
        );
      })}
    </TodoList>
  );
}

function renderRemaining(state: SimulatedTodoState) {
  const openItems = state.items.filter((item) => !item.isComplete);
  if (openItems.length === 0) {
    return <EmptyTodoAction message="No remaining todos." />;
  }

  return (
    <TodoList>
      {openItems.map((item) => (
        <TodoListRow
          key={item.id}
          icon={Circle}
          iconClassName={OPEN_TODO_ICON_CLASS}
          title={item.title}
        />
      ))}
    </TodoList>
  );
}

function renderAll(state: SimulatedTodoState) {
  if (state.items.length === 0) {
    return <EmptyTodoAction message="Todo list is empty." />;
  }

  return (
    <TodoList>
      {state.items.map((item) => (
        <TodoListRow
          key={item.id}
          icon={item.isComplete ? CircleCheck : Circle}
          iconClassName={item.isComplete ? COMPLETED_TODO_ICON_CLASS : OPEN_TODO_ICON_CLASS}
          title={item.title}
          titleClassName={item.isComplete ? COMPLETED_TODO_TITLE_CLASS : undefined}
          reason={item.isComplete ? item.completionReason : null}
        />
      ))}
    </TodoList>
  );
}

export function TodoToolCallView({ payload, state }: TodoToolCallViewProps) {
  switch (payload.name) {
    case "todos_add":
      return renderAdd(payload, state);
    case "todos_complete":
      return renderComplete(payload, state);
    case "todos_remove":
      return renderRemove(payload, state);
    case "todos_get_remaining":
      return renderRemaining(state);
    case "todos_get_all":
      return renderAll(state);
    default:
      return null;
  }
}
