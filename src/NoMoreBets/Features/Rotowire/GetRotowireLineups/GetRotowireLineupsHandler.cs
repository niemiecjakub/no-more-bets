using MediatR;
using NoMoreBets.Features.Rotowire.Model;
using NoMoreBets.Features.Rotowire.Scraping;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups;

/// <summary>
/// Handles <see cref="GetRotowireLineupsQuery"/> by delegating to <see cref="IRotowireScraper"/>.
/// </summary>
public class GetRotowireLineupsHandler : IRequestHandler<GetRotowireLineupsQuery, IReadOnlyList<GameLineup>>
{
    private readonly IRotowireScraper _scraper;

    public GetRotowireLineupsHandler(IRotowireScraper scraper)
    {
        _scraper = scraper;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GameLineup>> Handle(GetRotowireLineupsQuery request, CancellationToken cancellationToken)
    {
        return await _scraper.GetSoccerLineupsAsync(cancellationToken).ConfigureAwait(false);
    }
}
