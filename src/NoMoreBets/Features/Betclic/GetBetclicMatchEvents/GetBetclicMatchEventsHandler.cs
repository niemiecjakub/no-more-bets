using MediatR;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Betclic.Scraping;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents;

/// <summary>
/// Handles <see cref="GetBetclicMatchEventsQuery"/> by delegating to <see cref="IBetclicScraper"/>.
/// </summary>
public class GetBetclicMatchEventsHandler(IBetclicScraper scraper) : IRequestHandler<GetBetclicMatchEventsQuery, IReadOnlyList<BookmakerEvent>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BookmakerEvent>> Handle(GetBetclicMatchEventsQuery request, CancellationToken cancellationToken)
    {
        return await scraper.GetMatchEventsAsync(request.GameUrl, request.Expand, cancellationToken).ConfigureAwait(false);
    }
}
