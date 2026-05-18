using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseDefinition
{
  AgentSessionPhase Phase { get; }
  IReadOnlyList<AgentPhaseStep> Steps { get; }
}
