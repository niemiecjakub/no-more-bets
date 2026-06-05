using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseStep
{
  string BuildPrompt();
  IReadOnlyList<AITool> GetTools(IPluginFactory pluginFactory);
}
