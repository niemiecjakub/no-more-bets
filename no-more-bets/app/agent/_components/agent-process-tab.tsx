"use client";

import { useEffect, useState } from "react";
import type { JobGroup } from "@/features/jobs/interfaces";
import { fetchJobGroups } from "@/features/jobs/services/jobs-api";
import { handleServiceError } from "@/lib/error-handler";
import { AgentProcessWidget } from "./agent-process-widget";

export function AgentProcessTab() {
  const [jobGroups, setJobGroups] = useState<JobGroup[]>([]);
  const [isJobsLoading, setIsJobsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
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
        const message = handleServiceError(caughtError, "Failed to load agent data.");
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

  return (
    <AgentProcessWidget
      jobGroups={jobGroups}
      isJobsLoading={isJobsLoading}
      jobsError={error}
      activeStepGroup={activeStepGroup}
      onSelectStepGroup={setActiveStepGroup}
    />
  );
}
