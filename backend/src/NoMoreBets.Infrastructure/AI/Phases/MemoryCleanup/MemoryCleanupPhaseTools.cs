using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;

internal static class MemoryCleanupPhaseTools
{
  public static IReadOnlyList<AITool> CreateStepTools(IPluginFactory factory) => [];
}
