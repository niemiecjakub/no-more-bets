import { useState } from "react";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardMemoriesWidget,
  AgentDashboardPendingBetsWidget,
  AgentDashboardSessionsWidget,
} from "@/features/bets/interfaces";
import { formatCurrency } from "@/utils/format-currency";
import { AgentBettingSummaryDetailsPanel } from "./agent-betting-summary-details-panel";
import { AgentBankrollDetailsPanel } from "./agent-bankroll-details-panel";
import { AgentDashboardMemoriesDetailsPanel } from "./agent-dashboard-memories-details-panel";
import { AgentDashboardSessionsDetailsPanel } from "./agent-dashboard-sessions-details-panel";
import { AgentPendingBetsDetailsPanel } from "./agent-pending-bets-details-panel";
import { WidgetCard, WidgetSkeleton } from "./dashboard-widget-primitives";

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

interface AgentDashboardStatsWidgetsProps {
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
}

export function AgentDashboardStatsWidgets({
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
}: AgentDashboardStatsWidgetsProps) {
  const [activeWidget, setActiveWidget] = useState<"bankroll" | "summary" | "pending" | "sessions" | "memories">("bankroll");
  const bankrollBalance = bankrollWidget?.balance ?? 0;
  const isNegativeBankroll = bankrollBalance < 0;
  const bankrollAccentClassName = isNegativeBankroll ? "bg-red-500/80" : "bg-emerald-500/80";

  return (
    <section className="flex flex-col gap-3">
      <div className="grid gap-3 md:grid-cols-3 lg:grid-cols-5">
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
                ? "Total bankroll and betting balance"
                : "Dashboard data unavailable"
        }
        accentClassName={bankrollAccentClassName}
        isActive={activeWidget === "bankroll"}
        onClick={() => setActiveWidget("bankroll")}
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
        isActive={activeWidget === "summary"}
        onClick={() => setActiveWidget("summary")}
      />
      <WidgetCard
        title="Pending Bets"
        value={isPendingBetsLoading ? <WidgetSkeleton /> : `${pendingBetsWidget?.pendingSlipsCount ?? 0} open`}
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
        isActive={activeWidget === "pending"}
        onClick={() => setActiveWidget("pending")}
      />
      <WidgetCard
        title="Sessions"
        value={isSessionsLoading ? <WidgetSkeleton /> : `${sessionsWidget?.sessionsCount ?? 0} total`}
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
        isActive={activeWidget === "sessions"}
        onClick={() => setActiveWidget("sessions")}
      />
      <WidgetCard
        title="Memories"
        value={isMemoriesLoading ? <WidgetSkeleton /> : `${memoriesWidget?.memoriesCount ?? 0} saved`}
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
        isActive={activeWidget === "memories"}
        onClick={() => setActiveWidget("memories")}
      />
      </div>
      {activeWidget === "bankroll" ? (
        <AgentBankrollDetailsPanel />
      ) : null}
      {activeWidget === "summary" ? (
        <AgentBettingSummaryDetailsPanel />
      ) : null}
      {activeWidget === "pending" ? (
        <AgentPendingBetsDetailsPanel />
      ) : null}
      {activeWidget === "sessions" ? (
        <AgentDashboardSessionsDetailsPanel />
      ) : null}
      {activeWidget === "memories" ? (
        <AgentDashboardMemoriesDetailsPanel />
      ) : null}
    </section>
  );
}
