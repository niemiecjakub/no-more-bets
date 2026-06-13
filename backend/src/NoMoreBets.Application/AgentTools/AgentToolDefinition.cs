namespace NoMoreBets.Application.AgentTools;

public sealed record AgentToolDefinition(
  string Name,
  string DisplayName,
  AgentToolCategory Category,
  bool UsesSessionMatch = false);
