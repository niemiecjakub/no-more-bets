namespace NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;

public record AgentSessionMessageDto(int Id, int SessionId, int Ordinal, int Kind, string Text);
