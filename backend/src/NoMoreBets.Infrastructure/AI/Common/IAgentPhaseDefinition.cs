using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseStep
{
  string BuildPrompt();
  IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) => [];
  IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) => [];
}

public sealed record AgentPhaseStep(IAgentPhaseStep Implementation, bool PersistTranscript);
