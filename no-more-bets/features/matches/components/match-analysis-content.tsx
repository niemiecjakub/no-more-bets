"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

interface MatchAnalysisContentProps {
  content: string;
}

/**
 * Renders analysis content as markdown in a scrollable area for long text.
 */
export function MatchAnalysisContent({ content }: MatchAnalysisContentProps) {
  return (
    <div className="max-h-[28rem] overflow-y-auto rounded-md border border-zinc-200 bg-zinc-50/50 px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900/50">
      <div className="text-sm text-foreground [&_p]:my-2 [&_ul]:my-2 [&_ol]:my-2 [&_li]:my-0.5 [&_strong]:font-semibold [&_a]:text-violet-600 dark:[&_a]:text-violet-400 [&_a]:underline [&_pre]:overflow-x-auto [&_code]:rounded [&_code]:bg-zinc-200 [&_code]:px-1 dark:[&_code]:bg-zinc-700">
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
      </div>
    </div>
  );
}
