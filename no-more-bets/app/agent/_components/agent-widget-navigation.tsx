"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardMemoriesWidget,
  AgentDashboardPendingBetsWidget,
  AgentDashboardSessionsWidget,
} from "@/features/bets/interfaces";
import { formatCurrency } from "@/utils/format-currency";
import { WidgetCard, WidgetSkeleton } from "./widget-primitives";

function paydayLabel(days: number): string {
  if (days === 0) return "Payday today";
  if (days === 1) return "1 day until payday";
  return `${days} days until payday`;
}

function formatRelativeDate(value: string | null) {
  if (!value) return "N/A";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "N/A";

  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const minutes = Math.floor(diffMs / 60000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;

  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

interface AgentWidgetNavigationProps {
  bankrollWidget: AgentDashboardBankrollWidget | null;
  bettingSummaryWidget: AgentDashboardBettingSummaryWidget | null;
  pendingBetsWidget: AgentDashboardPendingBetsWidget | null;
  sessionsWidget: AgentDashboardSessionsWidget | null;
  memoriesWidget: AgentDashboardMemoriesWidget | null;
  isBankrollLoading: boolean;
  isBettingSummaryLoading: boolean;
  isPendingBetsLoading: boolean;
  isSessionsLoading: boolean;
  isMemoriesLoading: boolean;
  bankrollError: string | null;
  bettingSummaryError: string | null;
  pendingBetsError: string | null;
  sessionsError: string | null;
  memoriesError: string | null;
  activeWidget: AgentWidgetNavigationId;
  onSelectWidget: (widget: AgentWidgetNavigationId) => void;
}

export const AGENT_WIDGET_IDS = {
  BANKROLL: "bankroll",
  SUMMARY: "summary",
  PENDING: "pending",
  SESSIONS: "sessions",
  MEMORIES: "memories",
} as const;

export type AgentWidgetNavigationId = (typeof AGENT_WIDGET_IDS)[keyof typeof AGENT_WIDGET_IDS];

export function AgentWidgetNavigation({
  bankrollWidget,
  bettingSummaryWidget,
  pendingBetsWidget,
  sessionsWidget,
  memoriesWidget,
  isBankrollLoading,
  isBettingSummaryLoading,
  isPendingBetsLoading,
  isSessionsLoading,
  isMemoriesLoading,
  bankrollError,
  bettingSummaryError,
  pendingBetsError,
  sessionsError,
  memoriesError,
  activeWidget,
  onSelectWidget,
}: AgentWidgetNavigationProps) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [showCarouselIndicator, setShowCarouselIndicator] = useState(false);
  const [scrollProgress, setScrollProgress] = useState(0);

  const updateCarouselState = useCallback(() => {
    const container = scrollRef.current;
    if (!container) return;

    const isCarouselViewport = !window.matchMedia("(min-width: 1024px)").matches;
    const maxScroll = container.scrollWidth - container.clientWidth;
    const isScrollable = maxScroll > 1;

    setShowCarouselIndicator(isCarouselViewport && isScrollable);
    setScrollProgress(isScrollable ? container.scrollLeft / maxScroll : 0);
  }, []);

  useEffect(() => {
    const container = scrollRef.current;
    if (!container) return;

    const frame = requestAnimationFrame(updateCarouselState);

    container.addEventListener("scroll", updateCarouselState, { passive: true });
    window.addEventListener("resize", updateCarouselState);

    const mediaQuery = window.matchMedia("(min-width: 1024px)");
    mediaQuery.addEventListener("change", updateCarouselState);

    const resizeObserver = new ResizeObserver(updateCarouselState);
    resizeObserver.observe(container);

    return () => {
      cancelAnimationFrame(frame);
      container.removeEventListener("scroll", updateCarouselState);
      window.removeEventListener("resize", updateCarouselState);
      mediaQuery.removeEventListener("change", updateCarouselState);
      resizeObserver.disconnect();
    };
  }, [updateCarouselState]);

  const bankrollBalance = bankrollWidget?.balance ?? 0;
  const isNegativeBankroll = bankrollBalance < 0;
  const bankrollAccentClassName = isNegativeBankroll ? "bg-red-500/80" : "bg-emerald-500/80";

  return (
    <section className="flex flex-col gap-3">
      <div
        ref={scrollRef}
        className="-mx-4 overflow-x-auto px-4 [-ms-overflow-style:none] [scrollbar-width:none] sm:-mx-6 sm:px-6 lg:mx-0 lg:overflow-visible lg:px-0 [&::-webkit-scrollbar]:hidden"
      >
        <div className="grid auto-cols-[66%] grid-flow-col gap-3 snap-x snap-mandatory [&>button]:snap-start md:auto-cols-[38%] lg:grid-flow-row lg:auto-cols-auto lg:grid-cols-5 lg:snap-none">
      <WidgetCard
        title="Bankroll"
        value={
          isBankrollLoading ? (
            <WidgetSkeleton />
          ) : bankrollWidget ? (
            <div className="flex items-baseline gap-2">
              <span>{formatCurrency(bankrollWidget.totalValue)}</span>
              <span
                className={`text-sm font-medium ${
                  bankrollWidget.balance < 0
                    ? "text-red-700 dark:text-red-300"
                    : bankrollWidget.balance > 0
                      ? "text-emerald-700 dark:text-emerald-300"
                      : "text-zinc-600 dark:text-zinc-300"
                }`}
              >
                ({formatCurrency(bankrollWidget.balance)})
              </span>
            </div>
          ) : (
            "N/A"
          )
        }
        meta={
          isBankrollLoading
            ? "Loading bankroll..."
            : bankrollError
              ? bankrollError
              : bankrollWidget
                ? (
                    <div className="flex flex-col gap-0.5">
                      <span className="block">Total bankroll and betting balance</span>
                      <span className="block text-zinc-500 dark:text-zinc-400">
                        {paydayLabel(bankrollWidget.daysUntilPayday)}
                      </span>
                    </div>
                  )
                : "Dashboard data unavailable"
        }
        accentClassName={bankrollAccentClassName}
        isActive={activeWidget === AGENT_WIDGET_IDS.BANKROLL}
        onClick={() => onSelectWidget(AGENT_WIDGET_IDS.BANKROLL)}
      />
      <WidgetCard
        title="Betting Summary"
        value={
          isBettingSummaryLoading ? (
            <WidgetSkeleton />
          ) : (
            <div className="flex items-baseline gap-2">
              <span>{bettingSummaryWidget?.settledSlipsCount ?? 0} slips</span>
              <span className="text-sm font-medium text-zinc-600 dark:text-zinc-300">
                {bettingSummaryWidget?.settledSelectionsCount ?? 0} selections
              </span>
            </div>
          )
        }
        meta={
          isBettingSummaryLoading ? (
            "Loading summary..."
          ) : bettingSummaryError ? (
            bettingSummaryError
          ) : !bettingSummaryWidget || bettingSummaryWidget.settledSlipsCount === 0 ? (
            "No settled slips yet"
          ) : (
            <span>
              Win: {bettingSummaryWidget.winRatePercent.toFixed(1)}% | Loss:{" "}
              {bettingSummaryWidget.lossRatePercent.toFixed(1)}%
            </span>
          )
        }
        accentClassName="bg-sky-500/80"
        isActive={activeWidget === AGENT_WIDGET_IDS.SUMMARY}
        onClick={() => onSelectWidget(AGENT_WIDGET_IDS.SUMMARY)}
      />
      <WidgetCard
        title="Pending Bets"
        value={isPendingBetsLoading ? <WidgetSkeleton /> : `${pendingBetsWidget?.pendingSlipsCount ?? 0}`}
        meta={
          isPendingBetsLoading
            ? "Loading pending bets..."
            : pendingBetsError
              ? pendingBetsError
              : !pendingBetsWidget || pendingBetsWidget.pendingSlipsCount === 0
                ? "No pending bets"
                : `Staked ${formatCurrency(pendingBetsWidget.pendingStakeTotal)} / Payout ${formatCurrency(pendingBetsWidget.pendingPotentialPayoutTotal)}`
        }
        accentClassName="bg-amber-500/80"
        isActive={activeWidget === AGENT_WIDGET_IDS.PENDING}
        onClick={() => onSelectWidget(AGENT_WIDGET_IDS.PENDING)}
      />
      <WidgetCard
        title="Sessions"
        value={isSessionsLoading ? <WidgetSkeleton /> : `${sessionsWidget?.sessionsCount ?? 0}`}
        meta={
          isSessionsLoading
            ? "Loading sessions..."
            : sessionsError
              ? sessionsError
              : !sessionsWidget || sessionsWidget.sessionsCount === 0
                ? "No sessions recorded yet"
                : `${sessionsWidget.latestPhaseName ?? "Latest session"}${
                    sessionsWidget.latestStartedAt ? `, ${formatRelativeDate(sessionsWidget.latestStartedAt)}` : ""
                  }`
        }
        accentClassName="bg-violet-500/80"
        isActive={activeWidget === AGENT_WIDGET_IDS.SESSIONS}
        onClick={() => onSelectWidget(AGENT_WIDGET_IDS.SESSIONS)}
      />
      <WidgetCard
        title="Memories"
        value={isMemoriesLoading ? <WidgetSkeleton /> : `${memoriesWidget?.memoriesCount ?? 0}`}
        meta={
          isMemoriesLoading
            ? "Loading memories..."
            : memoriesError
              ? memoriesError
              : !memoriesWidget || memoriesWidget.memoriesCount === 0
                ? "No memories saved yet"
                : memoriesWidget.latestUpdatedAt
                  ? `Updated ${formatRelativeDate(memoriesWidget.latestUpdatedAt)}`
                  : "Latest memory available"
        }
        accentClassName="bg-fuchsia-500/80"
        isActive={activeWidget === AGENT_WIDGET_IDS.MEMORIES}
        onClick={() => onSelectWidget(AGENT_WIDGET_IDS.MEMORIES)}
      />
        </div>
      </div>
      {showCarouselIndicator ? (
        <div
          className="flex justify-center lg:hidden"
          role="progressbar"
          aria-label="Widget scroll position"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={Math.round(scrollProgress * 100)}
        >
          <div className="relative h-1 w-16 rounded-full bg-zinc-200 dark:bg-zinc-700">
            <div
              className="absolute inset-y-0 w-4 rounded-full bg-zinc-900 transition-[left] duration-150 dark:bg-zinc-100"
              style={{ left: `${scrollProgress * 75}%` }}
            />
          </div>
        </div>
      ) : null}
    </section>
  );
}
