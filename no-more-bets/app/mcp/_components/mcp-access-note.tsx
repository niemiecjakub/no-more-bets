"use client";

import { useState } from "react";
import { FeedbackSheet } from "@/features/feedback/components/feedback-sheet";

const linkClassName =
  "font-medium text-foreground underline underline-offset-2 hover:text-zinc-700 dark:hover:text-zinc-200";

export function McpAccessNote() {
  const [feedbackOpen, setFeedbackOpen] = useState(false);

  return (
    <>
      <p className="mt-4 text-balance text-base leading-7 text-zinc-500 dark:text-zinc-400 sm:text-lg">
        If you would like access, contact me on{" "}
        <a
          href="https://github.com/niemiecjakub/no-more-bets"
          target="_blank"
          rel="noreferrer noopener"
          className={linkClassName}
        >
          GitHub
        </a>{" "}
        or via{" "}
        <button
          type="button"
          onClick={() => setFeedbackOpen(true)}
          className={linkClassName}
        >
          Feedback
        </button>
        .
      </p>
      <FeedbackSheet open={feedbackOpen} onOpenChange={setFeedbackOpen} />
    </>
  );
}
