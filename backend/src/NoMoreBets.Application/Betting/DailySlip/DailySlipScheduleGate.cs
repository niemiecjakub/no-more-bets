using NoMoreBets.Application.Betting.GetMatchesAvailableForDailySlip;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using MediatR;

namespace NoMoreBets.Application.Betting.DailySlip;

public sealed class DailySlipScheduleGate(IMediator mediator, IUnitOfWork unitOfWork)
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
    var hasPick = await unitOfWork.Betting
      .AnyDailyPickOnDateAsync(cardDate, cancellationToken)
      .ConfigureAwait(false);
    if (hasPick)
    {
      return "a daily pick already exists for today";
    }

    var startUtc = DateTime.SpecifyKind(cardDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    var endUtc = startUtc.AddDays(1);
    var hasSession = await unitOfWork.AgentSessions
      .AnySessionInRangeAsync(AgentSessionPhase.DailySlip, startUtc, endUtc, cancellationToken)
      .ConfigureAwait(false);
    if (hasSession)
    {
      return "a daily slip session already exists for today";
    }

    return null;
  }
}
