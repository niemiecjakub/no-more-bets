using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;

public interface IAgentPhaseRunner
{
  Task<IReadOnlyList<IMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<IMessage>> RunUpcomingMatchesInternetResearchAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<IMessage>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<IMessage>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<IMessage>> RunMemoryCleanupPhaseAsync(CancellationToken cancellationToken = default);
}
