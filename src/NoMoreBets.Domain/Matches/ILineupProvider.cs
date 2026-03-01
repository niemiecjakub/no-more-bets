namespace NoMoreBets.Domain.Matches;
public interface ILineupProvider
{
  public async Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync();
}
