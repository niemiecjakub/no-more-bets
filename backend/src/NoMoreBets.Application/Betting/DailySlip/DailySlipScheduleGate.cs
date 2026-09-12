using NoMoreBets.Application.Betting.GetMatchesAvailableForDailySlip;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.AgentSessions;
using MediatR;

namespace NoMoreBets.Application.Betting.DailySlip;

public sealed class DailySlipScheduleGate(IMediator mediator, IBettingRepository betting, IAgentSessionRepository agentSessions)
{
  public async Task<string?> GetSkipReasonAsync(DateTime utcNow, CancellationToken cancellationToken = default)
  {
    var matches = await mediator
      .Send(new GetMatchesAvailableForDailySlipQuery(utcNow), cancellationToken)
      .ConfigureAwait(false);

    if (matches.Count == 0)
    {
      return "no matches available for today's card";
    }

    var cardDate = DateOnly.FromDateTime(utcNow);
    var hasPick = await betting
      .AnyDailyPickOnDateAsync(cardDate, cancellationToken)
      .ConfigureAwait(false);
    if (hasPick)
    {
      return "a daily pick already exists for today";
    }

    var startUtc = DateTime.SpecifyKind(cardDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    var endUtc = startUtc.AddDays(1);
    var hasSession = await agentSessions
      .AnySessionInRangeAsync(AgentSessionPhase.DailySlip, startUtc, endUtc, cancellationToken)
      .ConfigureAwait(false);
    if (hasSession)
    {
      return "a daily slip session already exists for today";
    }

    return null;
  }
}
