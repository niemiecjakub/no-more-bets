"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import type {
  AgentDashboardBankrollWidget,
  AgentDashboardBettingSummaryWidget,
  AgentDashboardMemoriesWidget,
  AgentDashboardPendingBetsWidget,
  AgentDashboardSessionsWidget,
} from "@/features/bets/interfaces";
import {
  fetchAgentDashboardBankrollWidget,
  fetchAgentDashboardBettingSummaryWidget,
  fetchAgentDashboardMemoriesWidget,
  fetchAgentDashboardPendingBetsWidget,
  fetchAgentDashboardSessionsWidget,
} from "@/features/bets/services/agent-dashboard-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentBankrollDetailsPanel } from "./_components/agent-bankroll-details-panel";
import { AgentBettingSummaryDetailsPanel } from "./_components/agent-betting-summary-details-panel";
import { AgentDashboardMemoriesDetailsPanel } from "./_components/agent-dashboard-memories-details-panel";
import {
  AgentDashboardSessionsDetailsPanel,
  type AgentDashboardSessionsDetailsPanelProps,
} from "./_components/agent-dashboard-sessions-details-panel";
import { AgentDashboardTab } from "./_components/agent-dashboard-tab";
import { AgentPendingBetsDetailsPanel } from "./_components/agent-pending-bets-details-panel";
import {
  AGENT_WIDGET_IDS,
  AgentWidgetNavigation,
  type AgentWidgetNavigationId,
} from "./_components/agent-widget-navigation";

const WIDGET_DETAILS_PANEL_RENDERERS: Record<
  AgentWidgetNavigationId,
  (props: AgentDashboardSessionsDetailsPanelProps) => ReactNode
> = {
  [AGENT_WIDGET_IDS.BANKROLL]: () => <AgentBankrollDetailsPanel />,
  [AGENT_WIDGET_IDS.SUMMARY]: () => <AgentBettingSummaryDetailsPanel />,
  [AGENT_WIDGET_IDS.PENDING]: () => <AgentPendingBetsDetailsPanel />,
  [AGENT_WIDGET_IDS.SESSIONS]: (props) => <AgentDashboardSessionsDetailsPanel {...props} />,
  [AGENT_WIDGET_IDS.MEMORIES]: () => <AgentDashboardMemoriesDetailsPanel />,
};

export default function AgentPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [activeWidget, setActiveWidget] = useState<AgentWidgetNavigationId>(AGENT_WIDGET_IDS.BANKROLL);
  const [bankrollWidget, setBankrollWidget] = useState<AgentDashboardBankrollWidget | null>(null);
  const [bettingSummaryWidget, setBettingSummaryWidget] = useState<AgentDashboardBettingSummaryWidget | null>(null);
  const [pendingBetsWidget, setPendingBetsWidget] = useState<AgentDashboardPendingBetsWidget | null>(null);
  const [sessionsWidget, setSessionsWidget] = useState<AgentDashboardSessionsWidget | null>(null);
  const [memoriesWidget, setMemoriesWidget] = useState<AgentDashboardMemoriesWidget | null>(null);
  const [isBankrollLoading, setIsBankrollLoading] = useState(true);
  const [isBettingSummaryLoading, setIsBettingSummaryLoading] = useState(true);
  const [isPendingBetsLoading, setIsPendingBetsLoading] = useState(true);
  const [isSessionsLoading, setIsSessionsLoading] = useState(true);
  const [isMemoriesLoading, setIsMemoriesLoading] = useState(true);
  const [bankrollError, setBankrollError] = useState<string | null>(null);
  const [bettingSummaryError, setBettingSummaryError] = useState<string | null>(null);
  const [pendingBetsError, setPendingBetsError] = useState<string | null>(null);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);
  const initialSessionId = useMemo(() => {
    const rawSessionId = searchParams.get("sessionId");
    if (rawSessionId == null) return null;
    const parsed = Number.parseInt(rawSessionId, 10);
    return Number.isFinite(parsed) ? parsed : null;
  }, [searchParams]);

  useEffect(() => {
    const requestedWidget = searchParams.get("widget");
    if (!requestedWidget) return;
    if (Object.values(AGENT_WIDGET_IDS).includes(requestedWidget as AgentWidgetNavigationId))
      setActiveWidget(requestedWidget as AgentWidgetNavigationId);
  }, [searchParams]);

  useEffect(() => {
    const requestedWidget = searchParams.get("widget");
    if (requestedWidget) return;
    const params = new URLSearchParams(searchParams.toString());
    params.set("widget", AGENT_WIDGET_IDS.BANKROLL);
    router.replace(`${pathname}?${params.toString()}`);
  }, [pathname, router, searchParams]);

  const handleSelectWidget = useCallback(
    (widget: AgentWidgetNavigationId) => {
      setActiveWidget(widget);
      const params = new URLSearchParams(searchParams.toString());
      params.set("widget", widget);
      if (widget !== AGENT_WIDGET_IDS.SESSIONS) params.delete("sessionId");
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams]
  );

  useEffect(() => {
    let cancelled = false;
    setIsBankrollLoading(true);
    setBankrollError(null);

    fetchAgentDashboardBankrollWidget()
      .then((data) => {
        if (!cancelled) setBankrollWidget(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setBankrollError(handleServiceError(caughtError, "Failed to load bankroll widget."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsBankrollLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsBettingSummaryLoading(true);
    setBettingSummaryError(null);

    fetchAgentDashboardBettingSummaryWidget()
      .then((data) => {
        if (!cancelled) setBettingSummaryWidget(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setBettingSummaryError(handleServiceError(caughtError, "Failed to load betting summary widget."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsBettingSummaryLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsPendingBetsLoading(true);
    setPendingBetsError(null);

    fetchAgentDashboardPendingBetsWidget()
      .then((data) => {
        if (!cancelled) setPendingBetsWidget(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setPendingBetsError(handleServiceError(caughtError, "Failed to load pending bets widget."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsPendingBetsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsSessionsLoading(true);
    setSessionsError(null);

    fetchAgentDashboardSessionsWidget()
      .then((data) => {
        if (!cancelled) setSessionsWidget(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setSessionsError(handleServiceError(caughtError, "Failed to load sessions widget."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsSessionsLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    let cancelled = false;
    setIsMemoriesLoading(true);
    setMemoriesError(null);

    fetchAgentDashboardMemoriesWidget()
      .then((data) => {
        if (!cancelled) setMemoriesWidget(data);
      })
      .catch((caughtError) => {
        if (!cancelled) {
          setMemoriesError(handleServiceError(caughtError, "Failed to load memories widget."));
        }
      })
      .finally(() => {
        if (!cancelled) setIsMemoriesLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950">
      <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6">
        <h1 className="mb-4 text-2xl font-semibold tracking-tight text-foreground">Agent</h1>
        <div className="flex flex-col gap-4">
          <AgentWidgetNavigation
            bankrollWidget={bankrollWidget}
            bettingSummaryWidget={bettingSummaryWidget}
            pendingBetsWidget={pendingBetsWidget}
            sessionsWidget={sessionsWidget}
            memoriesWidget={memoriesWidget}
            isBankrollLoading={isBankrollLoading}
            isBettingSummaryLoading={isBettingSummaryLoading}
            isPendingBetsLoading={isPendingBetsLoading}
            isSessionsLoading={isSessionsLoading}
            isMemoriesLoading={isMemoriesLoading}
            bankrollError={bankrollError}
            bettingSummaryError={bettingSummaryError}
            pendingBetsError={pendingBetsError}
            sessionsError={sessionsError}
            memoriesError={memoriesError}
            activeWidget={activeWidget}
            onSelectWidget={handleSelectWidget}
          />
          {WIDGET_DETAILS_PANEL_RENDERERS[activeWidget]({
            initialSelectedSessionId: initialSessionId,
          })}
          <AgentDashboardTab />
        </div>
      </main>
    </div>
  );
}
