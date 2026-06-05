using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.AI.Phases.Test;

public sealed class TestPhase : IAgentPhaseDefinition, IAgentPhaseStep
{
  public AgentSessionPhase Phase => AgentSessionPhase.Test;
  public IReadOnlyList<AgentPhaseStep> Steps => [new AgentPhaseStep(this, PersistTranscript: true)];

  public string BuildPrompt() => $"""
          How much money do you have and when will you be paid next?
          """;

  public IReadOnlyList<AITool> GetTools(IPluginFactory pluginFactory) => [];
}
