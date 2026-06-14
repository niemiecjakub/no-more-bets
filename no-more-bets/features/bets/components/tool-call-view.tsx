"use client";

import {
  Brain,
  ClipboardList,
  FlaskConical,
  Globe,
  Share2,
  Ticket,
  Trophy,
  Wallet,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import type { ToolCallDisplay, WebSearchSourceLink, WebSearchSourcesToolCallMetadata } from "../services/agent-session-api";

export type ToolCategory =
  | "match"
  | "betting"
  | "researchbet"
  | "socialmedia"
  | "todo"
  | "bankroll"
  | "websearch"
  | "memories"
  | "unknown";

interface ToolCategoryBadgeStyle {
  icon: LucideIcon;
  className: string;
}

const CATEGORY_BADGE_STYLES: Record<ToolCategory, ToolCategoryBadgeStyle> = {
  match: {
    icon: Trophy,
    className:
      "border-orange-200 bg-orange-50 text-orange-800 dark:border-orange-700 dark:bg-orange-900/30 dark:text-orange-200",
  },
  betting: {
    icon: Ticket,
    className:
      "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-200",
  },
  researchbet: {
    icon: FlaskConical,
    className:
      "border-violet-200 bg-violet-50 text-violet-800 dark:border-violet-700 dark:bg-violet-900/30 dark:text-violet-200",
  },
  socialmedia: {
    icon: Share2,
    className:
      "border-sky-200 bg-sky-50 text-sky-800 dark:border-sky-700 dark:bg-sky-900/30 dark:text-sky-200",
  },
  todo: {
    icon: ClipboardList,
    className:
      "border-amber-200 bg-amber-50 text-amber-800 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-200",
  },
  bankroll: {
    icon: Wallet,
    className:
      "border-teal-200 bg-teal-50 text-teal-800 dark:border-teal-700 dark:bg-teal-900/30 dark:text-teal-200",
  },
  websearch: {
    icon: Globe,
    className:
      "border-cyan-200 bg-cyan-50 text-cyan-800 dark:border-cyan-700 dark:bg-cyan-900/30 dark:text-cyan-200",
  },
  memories: {
    icon: Brain,
    className:
      "border-indigo-200 bg-indigo-50 text-indigo-800 dark:border-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-200",
  },
  unknown: {
    icon: Wrench,
    className:
      "border-zinc-300 bg-zinc-100 text-zinc-800 dark:border-zinc-600 dark:bg-zinc-800 dark:text-zinc-200",
  },
};

export interface ToolCallBadgeStyle {
  label: string;
  icon: LucideIcon;
  className: string;
}

function normalizeCategory(category: string): ToolCategory {
  return category in CATEGORY_BADGE_STYLES ? (category as ToolCategory) : "unknown";
}

export function getToolCallBadgeStyle(display: ToolCallDisplay): ToolCallBadgeStyle {
  const { icon, className } = CATEGORY_BADGE_STYLES[normalizeCategory(display.category)];
  return { label: display.label, icon, className };
}

interface ToolCallViewProps {
  display: ToolCallDisplay;
}

function stripWwwPrefix(hostname: string): string {
  return hostname.replace(/^www\./i, "");
}

function isWebSearchSourcesMetadata(metadata: { type: string }): metadata is WebSearchSourcesToolCallMetadata {
  return metadata.type === "webSearchSources";
}

function getWebSearchSources(display: ToolCallDisplay): WebSearchSourceLink[] {
  return (
    display.metadata
      ?.filter(isWebSearchSourcesMetadata)
      .flatMap((metadata) => metadata.sources) ?? []
  );
}

function formatWebSearchSourceLabel(source: WebSearchSourceLink): string {
  const parts: string[] = [];
  if (source.hostname?.trim()) parts.push(stripWwwPrefix(source.hostname.trim()));
  if (source.title?.trim()) parts.push(source.title.trim());
  return parts.length > 0 ? parts.join(" | ") : "Source";
}

export function ToolCallView({ display }: ToolCallViewProps) {
  const hasDetails = display.details != null && display.details.length > 0;
  const webSearchSources = getWebSearchSources(display);
  const hasSources = webSearchSources.length > 0;

  if (!hasDetails && !hasSources) {
    return null;
  }

  return (
    <div className="flex flex-col gap-1.5">
      {hasDetails ? (
        <ul className="flex flex-col gap-0.5">
          {display.details!.map((line, index) => (
            <li key={`${index}-${line}`} className="text-sm italic leading-5 text-zinc-600 dark:text-zinc-400">
              {line}
            </li>
          ))}
        </ul>
      ) : null}
      {hasSources ? (
        <ul className="list-disc space-y-0.5 pl-5">
          {webSearchSources.map((source, index) => {
            const label = formatWebSearchSourceLabel(source);
            const url = source.url?.trim();

            return (
              <li key={`${index}-${label}`} className="text-sm leading-5 text-zinc-600 dark:text-zinc-400">
                {url ? (
                  <a
                    href={url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-cyan-700 underline decoration-cyan-700/40 underline-offset-2 hover:text-cyan-800 dark:text-cyan-300 dark:decoration-cyan-300/40 dark:hover:text-cyan-200"
                  >
                    {label}
                  </a>
                ) : (
                  label
                )}
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
