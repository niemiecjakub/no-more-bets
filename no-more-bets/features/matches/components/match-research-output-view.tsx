"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { MatchResearchOutput } from "../interfaces";

const PROSE_CLASS =
  "text-sm text-foreground leading-6 [&_p]:my-2 [&_p:first-of-type]:mt-0 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700 wrap-break-word";

function ResearchSectionTitle({
  children,
  variant = "default",
}: {
  children: string;
  variant?: "default" | "warning";
}) {
  return (
    <h3
      className={
        variant === "warning"
          ? "text-xs font-semibold uppercase tracking-wider text-amber-700 dark:text-amber-300"
          : "text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400"
      }
    >
      {children}
    </h3>
  );
}

export interface MatchResearchOutputViewProps {
  research: MatchResearchOutput;
}

export function MatchResearchOutputView({ research }: MatchResearchOutputViewProps) {
  const overview = research.matchOverview.trim();
  const keyPoints = research.keyPoints.filter((p) => p.trim() !== "");
  const risks = research.risksAndUnknowns.filter((r) => r.trim() !== "");

  return (
    <div className="space-y-3">
      {overview !== "" ? (
        <section className="space-y-1.5">
          <ResearchSectionTitle>Overview</ResearchSectionTitle>
          <div className={PROSE_CLASS}>
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{overview}</ReactMarkdown>
          </div>
        </section>
      ) : null}
      {keyPoints.length > 0 ? (
        <section className="space-y-1.5">
          <ResearchSectionTitle>Key points</ResearchSectionTitle>
          <ul className="list-disc space-y-1 pl-5 text-sm leading-6 text-foreground">
            {keyPoints.map((point, index) => (
              <li key={index}>{point}</li>
            ))}
          </ul>
        </section>
      ) : null}
      {risks.length > 0 ? (
        <section className="space-y-1.5">
          <ResearchSectionTitle variant="warning">Risks & unknowns</ResearchSectionTitle>
          <ul className="list-disc space-y-1 pl-5 text-sm leading-6 text-foreground">
            {risks.map((risk, index) => (
              <li key={index}>{risk}</li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}
