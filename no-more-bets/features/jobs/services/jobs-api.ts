import axiosInstance from "@/lib/axios";
import type { JobGroup } from "../interfaces";

export async function fetchJobGroups(): Promise<JobGroup[]> {
  const { data } = await axiosInstance.get<JobGroup[]>("/api/jobs/groups");
  return data;
}
