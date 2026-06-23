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

  public async Task<AgentSessionPage> GetSessionsPageAsync(
    int limit,
    DateTime? afterStartedAtUtc,
    int? afterId,
    int? includeSessionId,
    IReadOnlyCollection<AgentSessionPhase>? phaseIds = null,
    CancellationToken cancellationToken = default)
  {
    var isFirstPage = afterStartedAtUtc is null && afterId is null;
    if (!isFirstPage)
      includeSessionId = null;

    var query = db.AgentSession.AsNoTracking();
    if (phaseIds is { Count: > 0 })
      query = query.Where(s => phaseIds.Contains(s.Phase));

    if (afterStartedAtUtc is not null && afterId is not null)
    {
      var cursorStartedAt = afterStartedAtUtc.Value;
      var cursorId = afterId.Value;
      query = query.Where(s =>
        s.StartedAt < cursorStartedAt
        || (s.StartedAt == cursorStartedAt && s.Id < cursorId));
    }

    var rows = await query
      .OrderByDescending(s => s.StartedAt)
      .ThenByDescending(s => s.Id)
      .Take(limit + 1)
      .Select(s => new AgentSessionListRow(
        s.Id,
        s.Phase,
        s.StartedAt,
        s.Messages.Count(m => m.Kind != AgentSessionMessageKind.FunctionCall)))
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var hasMore = rows.Count > limit;
    if (hasMore)
      rows.RemoveAt(rows.Count - 1);

    if (isFirstPage && includeSessionId is > 0 && rows.All(r => r.Id != includeSessionId.Value))
    {
      var included = await db.AgentSession
        .AsNoTracking()
        .Where(s => s.Id == includeSessionId.Value)
        .Select(s => new AgentSessionListRow(
          s.Id,
          s.Phase,
          s.StartedAt,
          s.Messages.Count(m => m.Kind != AgentSessionMessageKind.FunctionCall)))
        .SingleOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false);

      if (included is not null)
        rows.Add(included);
    }

    rows = rows
      .OrderByDescending(r => r.StartedAt)
      .ThenByDescending(r => r.Id)
      .ToList();

    return new AgentSessionPage(rows, hasMore);
  }

  public async Task<IReadOnlyDictionary<int, int>> GetMatchIdsBySessionIdsAsync(
    IReadOnlyCollection<int> sessionIds,
    CancellationToken cancellationToken = default)
  {
    if (sessionIds.Count == 0)
      return new Dictionary<int, int>();

    var pairs = await db.MatchAnalysis
      .AsNoTracking()
      .Where(a => a.AgentSessionId != null && sessionIds.Contains(a.AgentSessionId.Value))
      .Select(a => new { SessionId = a.AgentSessionId!.Value, a.MatchId })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return pairs
      .GroupBy(p => p.SessionId)
      .ToDictionary(g => g.Key, g => g.First().MatchId);
  }

  public async Task<IReadOnlyDictionary<int, AgentSessionMatchSummary>> GetMatchSummariesBySessionIdsAsync(
    IReadOnlyCollection<int> sessionIds,
    CancellationToken cancellationToken = default)
  {
    if (sessionIds.Count == 0)
      return new Dictionary<int, AgentSessionMatchSummary>();

    var rows = await db.MatchAnalysis
      .AsNoTracking()
      .Where(a => a.AgentSessionId != null && sessionIds.Contains(a.AgentSessionId.Value))
      .Select(a => new
      {
        SessionId = a.AgentSessionId!.Value,
        a.MatchId,
        a.Match.MatchDate,
        a.Match.MatchStatusId,
        a.Match.HomeGoals,
        a.Match.AwayGoals,
        HomeClubName = a.Match.HomeClub.Name,
        AwayClubName = a.Match.AwayClub.Name,
        HomeClubSlug = a.Match.HomeClub.Slug,
        AwayClubSlug = a.Match.AwayClub.Slug,
      })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return rows
      .GroupBy(r => r.SessionId)
      .ToDictionary(
        g => g.Key,
        g =>
        {
          var row = g.First();
          return new AgentSessionMatchSummary(
            row.MatchId,
            row.HomeClubName,
            row.AwayClubName,
            row.HomeClubSlug,
            row.AwayClubSlug,
            row.MatchDate,
            row.MatchStatusId,
            row.HomeGoals,
            row.AwayGoals);
        });
  }

  public Task<bool> SessionExistsAsync(int sessionId, CancellationToken cancellationToken = default) =>
    db.AgentSession.AsNoTracking().AnyAsync(s => s.Id == sessionId, cancellationToken);

  public async Task<IReadOnlyList<AgentSessionMessage>> GetMessagesAsync(
    int sessionId,
    CancellationToken cancellationToken = default)
  {
    return await db.AgentSessionMessage
      .AsNoTracking()
      .Where(m => m.SessionId == sessionId)
      .OrderBy(m => m.Ordinal)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<AgentSessionsWidgetData> GetSessionsWidgetAsync(CancellationToken cancellationToken = default)
  {
    var sessions = await db.AgentSession
      .AsNoTracking()
      .Select(s => new { s.StartedAt, s.Phase })
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    var latest = sessions.OrderByDescending(s => s.StartedAt).FirstOrDefault();

    return new AgentSessionsWidgetData(
      sessions.Count,
      latest?.StartedAt,
      latest?.Phase.ToString());
  }
}
