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

  public async Task AddReflectionScopeBetSlipsAsync(
    int sessionId,
    IReadOnlyList<int> betSlipIds,
    CancellationToken cancellationToken = default)
  {
    if (betSlipIds.Count == 0)
    {
      return;
    }

    var distinctIds = betSlipIds.Distinct().ToList();
    var existing = await db.AgentSessionReflectionBetSlip
      .Where(x => x.AgentSessionId == sessionId && distinctIds.Contains(x.BetSlipId))
      .Select(x => x.BetSlipId)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var existingSet = existing.ToHashSet();
    var rows = distinctIds
      .Where(id => !existingSet.Contains(id))
      .Select(betSlipId => new AgentSessionReflectionBetSlip
      {
        AgentSessionId = sessionId,
        BetSlipId = betSlipId
      })
      .ToList();

    if (rows.Count == 0)
    {
      return;
    }

    await db.AgentSessionReflectionBetSlip.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false);
    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }
}
