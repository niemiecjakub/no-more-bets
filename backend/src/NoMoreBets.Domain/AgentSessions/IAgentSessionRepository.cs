namespace NoMoreBets.Domain.AgentSessions;

public interface IAgentSessionRepository
{
  Task<int> CreateSessionAsync(AgentSessionPhase phase, DateTime startedAt, CancellationToken cancellationToken = default);

  Task AddMessagesAsync(
    int sessionId,
    IReadOnlyList<AgentSessionMessage> messages,
    CancellationToken cancellationToken = default);

  Task DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default);
}
