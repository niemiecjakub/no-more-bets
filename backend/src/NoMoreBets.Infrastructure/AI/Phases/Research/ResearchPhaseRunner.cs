using System.Text.Json;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;
using AgentSessionPhase = NoMoreBets.Domain.AgentSessions.AgentSessionPhase;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public sealed class ResearchPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<ResearchPhaseRunner> logger) : IAgentPhaseRunner
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<IReadOnlyList<IMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default)
  {
    var result = await AgentPhaseSessionRunner.RunAsync(
      AgentSessionPhase.Research,
      "Betting agent phase",
      agentBuilder,
      messageCollector,
      unitOfWork,
      agentSessionContext,
      serviceProvider,
      logger,
      async (runStep, _, ct) =>
      {
        var researchResult = await runStep(
          new ResearchExecuteStep(match),
          persistTranscript: true,
          typeof(MatchResearchOutput),
          null,
          ct).ConfigureAwait(false);

        var researchOutput = JsonSerializer.Deserialize<MatchResearchOutput>(
          researchResult.Response.Text,
          JsonOptions);
        if (researchOutput is null)
        {
          logger.LogWarning(
            "Research phase for match {MatchId} did not return parseable {OutputType}",
            match.Id,
            nameof(MatchResearchOutput));
        }
        else
        {
          var analysis = MatchAnalysis.CreateStructuredResearch(match.Id, agentSessionContext.SessionId!.Value, researchOutput);
          await unitOfWork.Matches.AddMatchAnalysisAsync(analysis, ct).ConfigureAwait(false);
          await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        try
        {
          await runStep(
            new PaperBetFollowUpStep(match.Id),
            persistTranscript: false,
            null,
            researchResult.Session,
            ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          logger.LogWarning(
            ex,
            "Paper bet follow-up step failed for match {MatchId}; research output was persisted",
            match.Id);
        }
      },
      cancellationToken).ConfigureAwait(false);

    return result.Messages;
  }
}
