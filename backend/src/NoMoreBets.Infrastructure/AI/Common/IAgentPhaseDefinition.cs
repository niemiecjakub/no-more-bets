using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseDefinition
{
  AgentSessionPhase Phase { get; }
  IReadOnlyList<AgentPhaseStep> Steps { get; }
}

public interface IAgentPhaseStep
{
  string BuildPrompt();
  IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) => [];
  IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) => [];
}

public sealed record AgentPhaseStep(IAgentPhaseStep Implementation, bool PersistTranscript);

public sealed record AgentPhaseRunResult(IReadOnlyList<IMessage> Messages, int? SessionId);
