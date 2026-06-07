using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers;

namespace NoMoreBets.Infrastructure.AI.Phases.Test;

public sealed class TestPhase : IAgentPhaseDefinition, IAgentPhaseStep
{
  public AgentSessionPhase Phase => AgentSessionPhase.Test;
  public IReadOnlyList<AgentPhaseStep> Steps => [new AgentPhaseStep(this, PersistTranscript: true)];

  public string BuildPrompt() => $"""
          How much money do you have and when will you be paid next?
          """;

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
  ];
}
