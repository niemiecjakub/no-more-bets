using System.Text.Json.Serialization;

namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WebSearchSourcesToolCallMetadataDto), "webSearchSources")]
public abstract record ToolCallMetadataDto;
