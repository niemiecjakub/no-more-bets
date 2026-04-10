using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;

public interface IAgentPhaseRunner
{
  Task<IReadOnlyList<BaseMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BaseMessage>> RunReflectionPhaseAsync(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<BaseMessage>> RunBettingExecutionPhaseAsync(CancellationToken cancellationToken = default);
}
