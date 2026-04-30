"use client";

import { useEffect, useState } from "react";
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
import type { JobGroup } from "@/features/jobs/interfaces";
import { fetchJobGroups } from "@/features/jobs/services/jobs-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentDashboardProcessWidget } from "./agent-dashboard-process-widget";
import { AgentDashboardStatsWidgets } from "./agent-dashboard-stats-widgets";

export function AgentDashboardTab() {
  const [jobGroups, setJobGroups] = useState<JobGroup[]>([]);
  const [bankrollWidget, setBankrollWidget] = useState<AgentDashboardBankrollWidget | null>(null);
  const [bettingSummaryWidget, setBettingSummaryWidget] = useState<AgentDashboardBettingSummaryWidget | null>(null);
  const [pendingBetsWidget, setPendingBetsWidget] = useState<AgentDashboardPendingBetsWidget | null>(null);
  const [sessionsWidget, setSessionsWidget] = useState<AgentDashboardSessionsWidget | null>(null);
  const [memoriesWidget, setMemoriesWidget] = useState<AgentDashboardMemoriesWidget | null>(null);
  const [isJobsLoading, setIsJobsLoading] = useState(true);
  const [isBankrollLoading, setIsBankrollLoading] = useState(true);
  const [isBettingSummaryLoading, setIsBettingSummaryLoading] = useState(true);
  const [isPendingBetsLoading, setIsPendingBetsLoading] = useState(true);
  const [isSessionsLoading, setIsSessionsLoading] = useState(true);
  const [isMemoriesLoading, setIsMemoriesLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [bankrollError, setBankrollError] = useState<string | null>(null);
  const [bettingSummaryError, setBettingSummaryError] = useState<string | null>(null);
  const [pendingBetsError, setPendingBetsError] = useState<string | null>(null);
  const [sessionsError, setSessionsError] = useState<string | null>(null);
  const [memoriesError, setMemoriesError] = useState<string | null>(null);
  const [activeStepGroup, setActiveStepGroup] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsJobsLoading(true);
      setError(null);
      try {
        const jobsData = await fetchJobGroups();
        if (!cancelled) {
          setJobGroups(jobsData);
          setActiveStepGroup(jobsData[0]?.group ?? null);
        }
      } catch (caughtError) {
        const message = handleServiceError(caughtError, "Failed to load dashboard data.");
        if (!cancelled) {
          setError(message);
        }
      } finally {
        if (!cancelled) {
          setIsJobsLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

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
    <div className="flex flex-col gap-4">
      <AgentDashboardStatsWidgets
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
      />
      <AgentDashboardProcessWidget
        jobGroups={jobGroups}
        isJobsLoading={isJobsLoading}
        jobsError={error}
        activeStepGroup={activeStepGroup}
        onSelectStepGroup={setActiveStepGroup}
      />
    </div>
  );
}
