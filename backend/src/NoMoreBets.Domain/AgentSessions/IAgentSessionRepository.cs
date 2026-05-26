namespace NoMoreBets.Domain.AgentSessions;

public interface IAgentSessionRepository
{
  Task<int> CreateSessionAsync(AgentSessionPhase phase, DateTime startedAt, CancellationToken cancellationToken = default);

  Task AddMessagesAsync(
    int sessionId,
    IReadOnlyList<AgentSessionMessage> messages,
    CancellationToken cancellationToken = default);

  Task DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default);
  Task<AgentSessionPage> GetSessionsPageAsync(
    int limit,
    DateTime? afterStartedAtUtc,
    int? afterId,
    int? includeSessionId,
    IReadOnlyCollection<AgentSessionPhase>? phaseIds = null,
    CancellationToken cancellationToken = default);
  Task<IReadOnlyDictionary<int, int>> GetMatchIdsBySessionIdsAsync(
    IReadOnlyCollection<int> sessionIds,
    CancellationToken cancellationToken = default);
  Task<bool> SessionExistsAsync(int sessionId, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<AgentSessionMessage>> GetMessagesAsync(
    int sessionId,
    CancellationToken cancellationToken = default);
  Task<AgentSessionsWidgetData> GetSessionsWidgetAsync(CancellationToken cancellationToken = default);
}
