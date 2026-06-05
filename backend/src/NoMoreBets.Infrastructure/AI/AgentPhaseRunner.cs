using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Phases.Betting;
using NoMoreBets.Infrastructure.AI.Phases.InternetResearch;
using NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;
using NoMoreBets.Infrastructure.AI.Phases.Reflection;
using NoMoreBets.Infrastructure.AI.Phases.Research;
namespace NoMoreBets.Infrastructure.AI;

public sealed class AgentPhaseRunner(
  ResearchPhaseRunner research,
  InternetResearchPhaseRunner internetResearch,
  MemoryCleanupPhaseRunner memoryCleanup,
  ReflectionPhaseRunner reflection,
  BettingPhaseRunner betting) : IAgentPhaseRunner
{
  public Task<IReadOnlyList<IMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default)
    => research.RunAsync(match, cancellationToken);

  public Task<IReadOnlyList<IMessage>> RunUpcomingMatchesInternetResearchAsync(CancellationToken cancellationToken = default)
    => internetResearch.RunAsync(cancellationToken);

  public Task<IReadOnlyList<IMessage>> RunMemoryCleanupPhaseAsync(CancellationToken cancellationToken = default)
    => memoryCleanup.RunAsync(cancellationToken);

  public Task<IReadOnlyList<IMessage>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default)
    => reflection.RunAsync(cancellationToken);

  public Task<IReadOnlyList<IMessage>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default)
    => betting.RunAsync(cancellationToken);
}
