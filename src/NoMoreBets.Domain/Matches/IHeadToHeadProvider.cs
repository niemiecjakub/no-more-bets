using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Domain.Matches;
public interface IHeadToHeadProvider
{
  public Task<HeadToHead> GetHeadToHeadAsync(int team1Id, int team2Id, CancellationToken cancellationToken = default);
}
