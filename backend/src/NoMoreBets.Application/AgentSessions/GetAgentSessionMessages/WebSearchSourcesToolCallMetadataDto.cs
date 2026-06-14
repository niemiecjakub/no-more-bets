namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

public sealed record WebSearchSourcesToolCallMetadataDto(
  IReadOnlyList<WebSearchSourceLinkDto> Sources)
  : ToolCallMetadataDto;

public sealed record WebSearchSourceLinkDto(
  string? Title,
  string? Hostname,
  string? Url);
