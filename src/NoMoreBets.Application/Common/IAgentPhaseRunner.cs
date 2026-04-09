using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;

public interface IAgentPhaseRunner
{
  Task<IReadOnlyList<string>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<string>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<string>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default);
}
