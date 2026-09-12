using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Common;

public interface IAgentPhaseRunner
{
  Task<IReadOnlyList<IMessage>> RunResearchPhaseAsync(Match match, CancellationToken cancellationToken = default);
}
