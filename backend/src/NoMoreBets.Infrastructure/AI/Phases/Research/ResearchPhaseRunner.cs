using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Middlewares.AgentResponseMapping;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public sealed class ResearchPhaseRunner(
  AgentBuilder agentBuilder,
  AgentRunMessageCollector messageCollector,
  IUnitOfWork unitOfWork,
  AgentSessionContext agentSessionContext,
  IServiceProvider serviceProvider,
  ILogger<ResearchPhaseRunner> logger)
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<IReadOnlyList<IMessage>> RunAsync(Match match, CancellationToken cancellationToken = default)
  {
    var phaseName = ResearchPhaseDefinition.Phase.ToString();
    logger.LogInformation("Betting agent phase {Phase} starting", phaseName);

    var startedAt = DateTime.UtcNow;
    var sessionId = await unitOfWork.AgentSessions
      .CreateSessionAsync(ResearchPhaseDefinition.Phase, startedAt, cancellationToken)
      .ConfigureAwait(false);
    agentSessionContext.SessionId = sessionId;

    var messages = new List<IMessage>();
    AgentSession? agentSession = null;
    try
    {
      var researchResult = await AgentPhaseStepExecutor.RunAsync(
        new ResearchExecuteStep(match),
        persistTranscript: true,
        responseFormatType: typeof(MatchResearchOutput),
        agentBuilder,
        messageCollector,
        serviceProvider,
        agentSession,
        messages,
        cancellationToken).ConfigureAwait(false);
      agentSession = researchResult.Session;

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
        var analysis = MatchAnalysis.CreateStructuredResearch(match.Id, sessionId, researchOutput);
        await unitOfWork.Matches.AddMatchAnalysisAsync(analysis, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      }

      var paperBetResult = await AgentPhaseStepExecutor.RunAsync(
        new PaperBetFollowUpStep(match.Id),
        persistTranscript: false,
        responseFormatType: null,
        agentBuilder,
        messageCollector,
        serviceProvider,
        agentSession,
        messages,
        cancellationToken).ConfigureAwait(false);
      agentSession = paperBetResult.Session;
    }
    finally
    {
      try
      {
        if (messages.Count == 0)
        {
          await unitOfWork.AgentSessions
            .DeleteSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);
        }
        else
        {
          var rows = AgentSessionTranscriptMapper.ToEntities(messages);
          await unitOfWork.AgentSessions
            .AddMessagesAsync(sessionId, rows, cancellationToken)
            .ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        if (messages.Count == 0)
        {
          logger.LogError(ex, "Failed to delete empty agent session {SessionId}", sessionId);
        }
        else
        {
          logger.LogError(ex, "Failed to persist agent session {SessionId} transcript", sessionId);
        }
      }

      agentSessionContext.SessionId = null;
    }

    logger.LogInformation(
      "Betting agent phase {Phase} completed with {MessageCount} assistant message(s)",
      phaseName,
      messages.Count);

    return messages;
  }
}
