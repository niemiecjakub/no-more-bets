namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

public record ToolCallDisplayDto(
  string Label,
  string Category,
  IReadOnlyList<string>? Details);
