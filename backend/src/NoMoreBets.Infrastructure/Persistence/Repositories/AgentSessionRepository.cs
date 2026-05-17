using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public sealed class AgentSessionRepository(AppDbContext db) : IAgentSessionRepository
{
  public async Task<int> CreateSessionAsync(
    AgentSessionPhase phase,
    DateTime startedAt,
    CancellationToken cancellationToken = default)
  {
    var session = new AgentSession
    {
      Phase = phase,
      StartedAt = startedAt
    };
    db.AgentSession.Add(session);
    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return session.Id;
  }

  public async Task AddMessagesAsync(
    int sessionId,
    IReadOnlyList<AgentSessionMessage> messages,
    CancellationToken cancellationToken = default)
  {
    if (messages.Count == 0)
    {
      return;
    }

    foreach (var message in messages)
    {
      message.SessionId = sessionId;
    }

    await db.AgentSessionMessage.AddRangeAsync(messages, cancellationToken).ConfigureAwait(false);
    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  public Task DeleteSessionAsync(int sessionId, CancellationToken cancellationToken = default)
  {
    return db.AgentSession
      .Where(s => s.Id == sessionId)
      .ExecuteDeleteAsync(cancellationToken);
  }
}
