using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.AI.Phases.DailySlip;

public sealed class DailySlipPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<DailySlipPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.DailySlip,
      "Daily slip phase",
      agentBuilder,
      messageCollector,
      unitOfWork,
      agentSessionContext,
      serviceProvider,
      logger,
      async (runStep, _, ct) =>
      {
        await runStep(new DailySlipExecuteStep(), persistTranscript: true, null, null, ct)
          .ConfigureAwait(false);
      },
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }
}
