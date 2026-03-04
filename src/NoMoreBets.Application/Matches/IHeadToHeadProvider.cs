using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Matches;

public interface IHeadToHeadProvider
{
  public Task<HeadToHead> GetHeadToHeadAsync(int team1Id, int team2Id, CancellationToken cancellationToken = default);
}
