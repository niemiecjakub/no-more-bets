using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Common;

public interface IAgentPhaseStep
{
  string BuildPrompt();
  void ConfigureKernel(Kernel kernel, IPluginFactory pluginFactory);
}
