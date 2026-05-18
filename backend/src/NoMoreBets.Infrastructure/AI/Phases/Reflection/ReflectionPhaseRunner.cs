using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.Reflection;

public sealed class ReflectionPhaseRunner(
  AgentPhaseExecutor executor,
  ReflectionPhase phase,
  IUnitOfWork unitOfWork,
  ILogger<ReflectionPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var slips = await unitOfWork.Betting
      .GetNonPendingBetSlipsAwaitingReflectionAsync(cancellationToken)
      .ConfigureAwait(false);
    if (slips.Count == 0)
    {
      logger.LogInformation(
        "Skipping reflection agent phase: no settled bet slips awaiting reflection (non-pending with no reflection session).");
      return Array.Empty<IMessage>();
    }

    var reflectionBetSlipIds = slips.Select(s => s.Id).ToList();

    var result = await executor.ExecuteAsync(phase, cancellationToken).ConfigureAwait(false);

    if (reflectionBetSlipIds.Count > 0 && result.SessionId is int reflectionSessionId)
    {
      await unitOfWork.Betting
        .MarkBetSlipsAgentSessionReflectedAsync(reflectionSessionId, reflectionBetSlipIds, cancellationToken)
        .ConfigureAwait(false);
    }

    return result.Messages;
  }
}
