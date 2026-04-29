"use client";

import { useEffect, useState } from "react";
import type { JobGroup } from "@/features/jobs/interfaces";
import { fetchJobGroups } from "@/features/jobs/services/jobs-api";
import { handleServiceError } from "@/lib/error-handler";

function parseTimeUntil(value: string | null) {
  if (!value) return null;
  // .NET TimeSpan JSON is usually "hh:mm:ss" or "d.hh:mm:ss".
  const match = /^(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.\d+)?$/.exec(value);
  if (!match) return null;

  const days = Number(match[1] ?? 0);
  const hours = Number(match[2] ?? 0);
  const minutes = Number(match[3] ?? 0);
  const totalHours = days * 24 + hours;
  const totalMinutes = totalHours * 60 + minutes;

  return {
    label: `${totalHours}h ${minutes}m`,
    totalMinutes,
  };
}

function timeUntilBadgeClass(value: string | null) {
  const parsed = parseTimeUntil(value);
  if (!parsed)
    return "border-zinc-200 bg-zinc-100 text-zinc-700 dark:border-zinc-700 dark:bg-zinc-800/70 dark:text-zinc-300";

  if (parsed.totalMinutes < 60)
    return "border-red-200 bg-red-100 text-red-800 dark:border-red-800 dark:bg-red-950/50 dark:text-red-200";

  if (parsed.totalMinutes < 6 * 60)
    return "border-amber-200 bg-amber-100 text-amber-800 dark:border-amber-800 dark:bg-amber-950/50 dark:text-amber-200";

  return "border-emerald-200 bg-emerald-100 text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-200";
}

function formatTimeUntil(value: string | null) {
  const parsed = parseTimeUntil(value);
  if (!parsed) return "N/A";
  return parsed.label;
}

export function AgentDashboardTab() {
  const [jobGroups, setJobGroups] = useState<JobGroup[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedStepGroup, setExpandedStepGroup] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await fetchJobGroups();
        if (!cancelled) {
          setJobGroups(data);
          setExpandedStepGroup(data[0]?.group ?? null);
        }
      } catch (caughtError) {
        if (!cancelled) {
          setError(handleServiceError(caughtError, "Failed to load dashboard jobs."));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  if (isLoading) {
    return (
      <p className="rounded-lg border border-zinc-200 bg-white px-4 py-3 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
        Loading dashboard...
      </p>
    );
  }

  if (error) {
    return (
      <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
        {error}
      </p>
    );
  }

  if (jobGroups.length === 0) {
    return (
      <p className="rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
        No job groups found.
      </p>
    );
  }

  const orderedGroups = [...jobGroups].sort((a, b) => {
    if (a.order !== b.order) {
      return a.order - b.order;
    }

    return a.group.localeCompare(b.group);
  });

  return (
    <div className="flex flex-col gap-4">
      <div className="space-y-3">
        {orderedGroups.map((group, index) => {
          const isExpanded = expandedStepGroup === group.group;

          return (
          <section
            key={group.group}
            className="overflow-hidden rounded-lg border border-zinc-200 bg-white transition-colors dark:border-zinc-800 dark:bg-zinc-950"
          >
            <button
              type="button"
              onClick={() => setExpandedStepGroup((current) => (current === group.group ? null : group.group))}
              className="flex w-full items-center justify-between gap-3 border-b border-zinc-100 px-4 py-3 text-left dark:border-zinc-800"
            >
              <div className="flex min-w-0 items-center gap-3">
                <span
                  className={`inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold ${
                    isExpanded
                      ? "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-950"
                      : "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
                  }`}
                >
                  {index + 1}
                </span>
                <div className="min-w-0">
                  <h2 className="truncate text-base font-semibold text-foreground">{group.group}</h2>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400">
                    {group.jobs.length} job{group.jobs.length === 1 ? "" : "s"}
                  </p>
                </div>
              </div>
              <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                {isExpanded ? "Collapse" : "Expand"}
              </span>
            </button>
            {isExpanded ? (
              <div className="overflow-x-auto">
              <table className="min-w-full table-fixed text-sm">
                <colgroup>
                  <col className="w-88" />
                  <col />
                  <col className="w-40" />
                </colgroup>
                <thead className="bg-zinc-50 text-left text-xs uppercase tracking-wide text-zinc-500 dark:bg-zinc-900/50 dark:text-zinc-400">
                  <tr>
                    <th className="px-4 py-2.5 font-medium">Job</th>
                    <th className="px-4 py-2.5 font-medium">Description</th>
                    <th className="px-4 py-2.5 font-medium">Next Run In</th>
                  </tr>
                </thead>
                <tbody>
                  {group.jobs.map((job) => (
                    <tr key={job.id} className="border-t border-zinc-100 align-top dark:border-zinc-800">
                      <td className="px-4 py-3">
                        <p className="font-medium text-foreground">{job.name}</p>
                      </td>
                      <td className="px-4 py-3 text-zinc-700 dark:text-zinc-300">
                        {job.description}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold ${timeUntilBadgeClass(job.timeUntilNextRun)}`}
                        >
                          {formatTimeUntil(job.timeUntilNextRun)}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              </div>
            ) : null}
          </section>
          );
        })}
      </div>
    </div>
  );
}
