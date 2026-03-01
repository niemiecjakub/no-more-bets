namespace NoMoreBets.Domain.Matches;
public interface IHeadToHeadProvider
{
  public async Task<HeadToHead> GetHeadToHeadAsync(int team1Id, int team2Id);
}
