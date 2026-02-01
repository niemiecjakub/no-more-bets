using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;

/// <summary>
/// Handles <see cref="GetFotmobLeagueTableQuery"/> by delegating to <see cref="IFotmobScraper"/>.
/// </summary>
public class GetFotmobLeagueTableHandler(IFotmobScraper scraper) : IRequestHandler<GetFotmobLeagueTableQuery, IReadOnlyList<ClubDto>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ClubDto>> Handle(GetFotmobLeagueTableQuery request, CancellationToken cancellationToken)
    {
        var clubs = await scraper.GetLeagueTableAsync(request.Filter, cancellationToken).ConfigureAwait(false);
        return clubs.Select(ClubDto.From).ToList();
    }
}
