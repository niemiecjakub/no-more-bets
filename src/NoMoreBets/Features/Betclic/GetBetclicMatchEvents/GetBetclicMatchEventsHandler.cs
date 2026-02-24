using MediatR;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Betclic.Scraping;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents;

/// <summary>
/// Handles <see cref="GetBetclicMatchEventsQuery"/> by delegating to <see cref="BetclicScraper"/>.
/// </summary>
public class GetBetclicMatchEventsHandler(BetclicScraper scraper) : IRequestHandler<GetBetclicMatchEventsQuery, IReadOnlyList<BookmakerEvent>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BookmakerEvent>> Handle(GetBetclicMatchEventsQuery request, CancellationToken cancellationToken)
    {
        return await scraper.GetMatchEventsAsync(request.GameUrl, request.Expand, cancellationToken).ConfigureAwait(false);
    }
}
