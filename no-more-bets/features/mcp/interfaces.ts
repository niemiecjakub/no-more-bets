/** Tool group from GET /api/mcp/tools. */
export interface McpTool {
  name: string;
  title: string;
  description: string;
}

export interface McpToolGroup {
  id: string;
  label: string;
  description: string;
  tools: McpTool[];
}
