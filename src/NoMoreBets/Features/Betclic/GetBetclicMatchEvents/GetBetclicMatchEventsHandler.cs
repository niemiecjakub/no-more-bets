using MediatR;
using NoMoreBets.Features.Betclic.Model;
using NoMoreBets.Features.Betclic.Scraping;

namespace NoMoreBets.Features.Betclic.GetBetclicMatchEvents;

/// <summary>
/// Query to fetch bookmaker events (markets) for a specific match from Betclic.
/// </summary>
/// <param name="BetclicGameUrl">URL to the match page.</param>
/// <param name="Expand">If true, clicks consent/modal and "see more" before parsing.</param>
public record GetBetclicMatchEventsQuery(string BetclicGameUrl, bool Expand = false) : IRequest<IReadOnlyList<BookmakerEvent>>;

/// <summary>
/// Handles <see cref="GetBetclicMatchEventsQuery"/> by delegating to <see cref="BetclicScraper"/>.
/// </summary>
public class GetBetclicMatchEventsHandler(BetclicScraper scraper, ILogger<GetBetclicMatchEventsHandler> logger) : IRequestHandler<GetBetclicMatchEventsQuery, IReadOnlyList<BookmakerEvent>>
{
  /// <inheritdoc />
  public async Task<IReadOnlyList<BookmakerEvent>> Handle(GetBetclicMatchEventsQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Betclic game {GameUrl} (Expand={Expand})",
      nameof(GetBetclicMatchEventsHandler),
      request.BetclicGameUrl,
      request.Expand);

    var result = await scraper.GetMatchEventsAsync(request.BetclicGameUrl, request.Expand, cancellationToken).ConfigureAwait(false);

    if (result.Count == 0)
    {
      logger.LogWarning(
        "Handler {HandlerName} fetched 0 bookmaker events for Betclic game {GameUrl}",
        nameof(GetBetclicMatchEventsHandler),
        request.BetclicGameUrl);
    }
    else
    {
      logger.LogInformation(
        "Handler {HandlerName} fetched {EventCount} bookmaker events for Betclic game {GameUrl}",
        nameof(GetBetclicMatchEventsHandler),
        result.Count,
        request.BetclicGameUrl);
    }

    return result;
  }
}
