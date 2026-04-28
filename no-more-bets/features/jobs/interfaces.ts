export interface JobInfo {
  id: string;
  name: string;
  description: string;
  timeUntilNextRun: string | null;
}

export interface JobGroup {
  group: string;
  jobs: JobInfo[];
}
