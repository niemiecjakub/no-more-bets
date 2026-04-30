import type { JobGroup } from "@/features/jobs/interfaces";
import { WidgetSkeleton } from "./dashboard-widget-primitives";

function parseTimeUntil(value: string | null) {
  if (!value) return null;
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

interface AgentDashboardProcessWidgetProps {
  jobGroups: JobGroup[];
  isJobsLoading: boolean;
  jobsError: string | null;
  activeStepGroup: string | null;
  onSelectStepGroup: (group: string) => void;
}

export function AgentDashboardProcessWidget({
  jobGroups,
  isJobsLoading,
  jobsError,
  activeStepGroup,
  onSelectStepGroup,
}: AgentDashboardProcessWidgetProps) {
  const orderedGroups = [...jobGroups].sort((a, b) => {
    if (a.order !== b.order) return a.order - b.order;
    return a.group.localeCompare(b.group);
  });

  const activeGroup = orderedGroups.find((group) => group.group === activeStepGroup) ?? orderedGroups[0] ?? null;
  const activeIndex = activeGroup
    ? orderedGroups.findIndex((group) => group.group === activeGroup.group)
    : -1;

  return (
    <section className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <header className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
        <h2 className="text-base font-semibold text-foreground">Process</h2>
      </header>
      {isJobsLoading ? (
        <div className="p-4">
          <WidgetSkeleton />
        </div>
      ) : jobsError ? (
        <p className="m-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200">
          {jobsError}
        </p>
      ) : jobGroups.length === 0 || !activeGroup ? (
        <p className="m-4 rounded-lg border border-zinc-200 bg-white px-4 py-6 text-center text-zinc-600 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-400">
          No job groups found.
        </p>
      ) : (
        <>
          <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
            <div className="flex w-full items-start pb-1">
              {orderedGroups.map((group, index) => {
                const isFilled = index <= activeIndex;
                return (
                  <div key={group.group} className="flex flex-1 items-start">
                    <button
                      type="button"
                      onClick={() => onSelectStepGroup(group.group)}
                      className="group inline-flex w-20 shrink-0 flex-col items-center text-center"
                    >
                      <span
                        className={`inline-flex h-8 w-8 items-center justify-center rounded-full border text-xs font-semibold transition-colors ${
                          isFilled
                            ? "border-zinc-900 bg-zinc-900 text-white dark:border-zinc-100 dark:bg-zinc-100 dark:text-zinc-950"
                            : "border-zinc-300 bg-white text-zinc-500 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-400"
                        }`}
                      >
                        {index + 1}
                      </span>
                      <span className="mt-1 text-xs font-medium text-zinc-700 dark:text-zinc-300">{group.group}</span>
                    </button>
                    {index < orderedGroups.length - 1 ? (
                      <div
                        className={`mt-4 h-0.5 min-w-10 flex-1 transition-colors ${
                          index < activeIndex ? "bg-zinc-900 dark:bg-zinc-100" : "bg-zinc-300 dark:bg-zinc-700"
                        }`}
                      />
                    ) : null}
                  </div>
                );
              })}
            </div>
          </div>
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
                {activeGroup.jobs.map((job) => (
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
        </>
      )}
    </section>
  );
}
