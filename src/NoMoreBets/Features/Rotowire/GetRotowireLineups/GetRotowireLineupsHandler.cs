using MediatR;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Infrastructure.Database;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>
/// Handles <see cref="GetRotowireLineupsQuery"/> by delegating to <see cref="IRotowireScraper"/>.
/// </summary>
public class GetRotowireLineupsHandler(IRotowireScraper scraper, AppDbContext db) : IRequestHandler<GetRotowireLineupsQuery, IReadOnlyList<GameLineup>>
{
  /// <inheritdoc />
  public async Task<IReadOnlyList<GameLineup>> Handle(GetRotowireLineupsQuery request, CancellationToken cancellationToken)
  {
    var lineups = await scraper.GetSoccerLineupsAsync(cancellationToken).ConfigureAwait(false);

    return lineups;
  }
}
