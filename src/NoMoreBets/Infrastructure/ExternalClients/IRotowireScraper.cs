using NoMoreBets.Domain.Entities.Rotowire;

namespace NoMoreBets.Infrastructure.ExternalClients;

/// <summary>
/// Fetches and parses soccer lineups from RotoWire (rotowire.com).
/// </summary>
public interface IRotowireScraper
{
    /// <summary>
    /// Gets soccer lineups (games with team lineups, injuries) for all leagues.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parsed game lineups.</returns>
    Task<IReadOnlyList<GameLineup>> GetSoccerLineupsAsync(CancellationToken cancellationToken = default);
}
