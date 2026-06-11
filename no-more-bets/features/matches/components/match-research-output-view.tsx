"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { MatchResearchOutput } from "../interfaces";

const PROSE_CLASS =
  "text-sm text-foreground [&_p]:my-2 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700";

function SectionBlock({
  label,
  children,
  variant = "default",
}: {
  label: string;
  children: React.ReactNode;
  variant?: "default" | "warning";
}) {
  const isWarning = variant === "warning";
  return (
    <section
      className={
        isWarning
          ? "rounded-lg border border-amber-200 bg-amber-50/60 px-4 py-3 dark:border-amber-800 dark:bg-amber-950/30"
          : "rounded-lg border border-zinc-200 bg-zinc-50/50 px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900/50"
      }
    >
      <h3
        className={
          isWarning
            ? "mb-2 text-xs font-semibold uppercase tracking-wider text-amber-700 dark:text-amber-300"
            : "mb-2 text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400"
        }
      >
        {label}
      </h3>
      <div className={PROSE_CLASS}>{children}</div>
    </section>
  );
}

function BulletList({ items }: { items: string[] }) {
  if (items.length === 0) return null;
  return (
    <ul className="list-disc space-y-1 pl-5 text-sm text-foreground">
      {items.map((item, index) => (
        <li key={index}>{item}</li>
      ))}
    </ul>
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
    <div className="flex w-full flex-col gap-4">
      {overview !== "" ? (
        <SectionBlock label="Overview">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{overview}</ReactMarkdown>
        </SectionBlock>
      ) : null}
      {keyPoints.length > 0 ? (
        <SectionBlock label="Key points">
          <BulletList items={keyPoints} />
        </SectionBlock>
      ) : null}
      {risks.length > 0 ? (
        <SectionBlock label="Risks & unknowns" variant="warning">
          <BulletList items={risks} />
        </SectionBlock>
      ) : null}
    </div>
  );
}
