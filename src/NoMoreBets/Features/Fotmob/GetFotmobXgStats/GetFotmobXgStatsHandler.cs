using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobXgStats;

/// <summary>
/// Query to fetch xG statistics table from FotMob.
/// </summary>
public record GetFotmobXgStatsQuery : IRequest<IReadOnlyList<XgStatsDto>>;

/// <summary>
/// Handles <see cref="GetFotmobXgStatsQuery"/> by delegating to <see cref="FotmobScraper"/>.
/// </summary>
public class GetFotmobXgStatsHandler(FotmobScraper scraper) : IRequestHandler<GetFotmobXgStatsQuery, IReadOnlyList<XgStatsDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<XgStatsDto>> Handle(GetFotmobXgStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await scraper.GetXgStatsAsync(cancellationToken).ConfigureAwait(false);
        return stats.Select(XgStatsDto.From).ToList();
    }
}
