using MediatR;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Betclic.Scraping;

namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames;

/// <summary>
/// Handles <see cref="GetBetclicUpcomingGamesQuery"/> by delegating to <see cref="IBetclicScraper"/>.
/// </summary>
public class GetBetclicUpcomingGamesHandler(IBetclicScraper scraper) : IRequestHandler<GetBetclicUpcomingGamesQuery, IReadOnlyList<UpcomingGame>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UpcomingGame>> Handle(GetBetclicUpcomingGamesQuery request, CancellationToken cancellationToken)
    {
        return await scraper.GetUpcomingGamesAsync(cancellationToken).ConfigureAwait(false);
    }
}
