"use client";

import { useState } from "react";
import { ChartPie } from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  ResearchBettingPanel,
  type ResearchBettingPanelProps,
} from "./research-betting-panel";

type ResearchBettingMobileSheetProps = Omit<ResearchBettingPanelProps, "showTitle">;

export function ResearchBettingMobileSheet(props: ResearchBettingMobileSheetProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        className="flex w-full items-center gap-2 rounded-lg border border-zinc-200 bg-white px-3.5 py-2.5 text-sm font-medium text-foreground transition-colors hover:border-zinc-300 hover:bg-zinc-50 active:bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:hover:border-zinc-700 dark:hover:bg-zinc-900"
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={() => setOpen(true)}
      >
        <ChartPie className="size-4 text-zinc-500 dark:text-zinc-400" aria-hidden />
        Research betting
      </button>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="bottom" className="max-h-[85dvh] overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Research betting</SheetTitle>
          </SheetHeader>
          <div className="px-4 pb-6">
            <ResearchBettingPanel {...props} showTitle={false} />
          </div>
        </SheetContent>
      </Sheet>
    </>
  );
}
