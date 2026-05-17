using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Matches;

public interface IMatchPreviewProvider
{
  public Task<MatchPreviewDto> GetMatchPreviewAsync(int soccerdataMatchId, CancellationToken cancellationToken = default);
}
