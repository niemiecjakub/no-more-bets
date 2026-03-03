using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Domain.Matches;
public interface IMatchPreviewProvider
{
  public Task<MatchPreviewDto> GetMatchPreviewAsync(int soccerdataMatchId, CancellationToken cancellationToken = default);
}
