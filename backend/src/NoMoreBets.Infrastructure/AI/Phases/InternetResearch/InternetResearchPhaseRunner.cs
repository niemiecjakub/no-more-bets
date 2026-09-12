using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public sealed class InternetResearchPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<InternetResearchPhaseRunner> logger)
{
  public async Task<IReadOnlyList<IMessage>> RunAsync(CancellationToken cancellationToken = default)
  {
    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.InternetResearch,
      "Betting agent phase",
      agentBuilder,
      messageCollector,
      unitOfWork,
      agentSessionContext,
      serviceProvider,
      logger,
      async (runStep, _, ct) =>
      {
        await runStep(new InternetResearchExecuteStep(), persistTranscript: true, null, null, ct)
          .ConfigureAwait(false);
      },
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }
}
