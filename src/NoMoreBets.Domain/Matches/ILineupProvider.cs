namespace NoMoreBets.Domain.Matches;
public interface ILineupProvider
{
  public Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync(CancellationToken cancellationToken = default);
}
