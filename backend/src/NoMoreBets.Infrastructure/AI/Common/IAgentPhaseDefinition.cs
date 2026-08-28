using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseStep
{
  string BuildPrompt();
  string? AgentInstructions => null;
  IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) => [];
  IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) => [];
}
