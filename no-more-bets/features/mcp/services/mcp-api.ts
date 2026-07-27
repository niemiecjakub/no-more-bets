import axiosInstance from "@/lib/axios";
import type { McpToolGroup } from "../interfaces";

export async function fetchMcpToolGroups(): Promise<McpToolGroup[]> {
  const { data } = await axiosInstance.get<McpToolGroup[]>("/api/mcp/tools");
  return data;
}
