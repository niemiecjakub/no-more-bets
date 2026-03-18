"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { StructuredMatchAnalysis } from "../interfaces";

const PROSE_CLASS =
  "text-sm text-foreground [&_p]:my-2 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700";

const SECTIONS: {
  key: keyof StructuredMatchAnalysis;
  label: string;
  isPrediction?: boolean;
}[] = [
  { key: "context", label: "Context" },
  { key: "form", label: "Form" },
  { key: "tactics", label: "Tactics" },
  { key: "squad", label: "Squad" },
  { key: "statistics", label: "Statistics" },
  { key: "market", label: "Market" },
  { key: "matchProjection", label: "Match projection" },
  { key: "prediction", label: "Prediction", isPrediction: true },
];

function SectionBlock({
  label,
  children,
  isPrediction,
}: {
  label: string;
  children: React.ReactNode;
  isPrediction?: boolean;
}) {
  return (
    <section
      className={
        isPrediction
          ? "rounded-lg border-2 border-violet-200 bg-violet-50/60 px-4 py-3 dark:border-violet-800 dark:bg-violet-950/30"
          : "rounded-lg border border-zinc-200 bg-zinc-50/50 px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900/50"
      }
    >
      <h3
        className={
          isPrediction
            ? "mb-2 text-xs font-semibold uppercase tracking-wider text-violet-700 dark:text-violet-300"
            : "mb-2 text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400"
        }
      >
        {label}
      </h3>
      <div className={PROSE_CLASS}>{children}</div>
    </section>
  );
}

export interface StructuredMatchAnalysisViewProps {
  analysis: StructuredMatchAnalysis;
}

/**
 * Renders structured match analysis in ordered sections with markdown.
 * Prediction section is visually emphasized.
 */
export function StructuredMatchAnalysisView({
  analysis,
}: StructuredMatchAnalysisViewProps) {
  return (
    <div className="flex w-full flex-col gap-4">
      {SECTIONS.map(({ key, label, isPrediction }) => {
        const value = analysis[key];
        if (value == null || String(value).trim() === "") return null;
        return (
          <SectionBlock
            key={key}
            label={label}
            isPrediction={isPrediction}
          >
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{value}</ReactMarkdown>
          </SectionBlock>
        );
      })}
    </div>
  );
}
