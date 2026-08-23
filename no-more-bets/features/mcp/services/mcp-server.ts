import { apiGetJson } from "@/lib/api-server";
import type { McpToolGroup } from "@/features/mcp/interfaces";

export async function getMcpToolGroups(): Promise<McpToolGroup[]> {
  const raw = await apiGetJson<McpToolGroup[]>("/api/mcp/tools");
  return raw ?? [];
}
