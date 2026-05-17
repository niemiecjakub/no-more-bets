namespace NoMoreBets.Domain.Matches;

public interface ILineupProvider
{
  IReadOnlyCollection<string> SupportedLeagueSlugs { get; }
  Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync(string leagueSlug, CancellationToken cancellationToken = default);
}
