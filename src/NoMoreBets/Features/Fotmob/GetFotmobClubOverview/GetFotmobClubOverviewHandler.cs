using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubOverview;

/// <summary>
/// Handles <see cref="GetFotmobClubOverviewQuery"/> by delegating to <see cref="IFotmobScraper"/>.
/// </summary>
public class GetFotmobClubOverviewHandler(IFotmobScraper scraper) : IRequestHandler<GetFotmobClubOverviewQuery, ClubOverviewDto>
{
    /// <inheritdoc />
    public async Task<ClubOverviewDto> Handle(GetFotmobClubOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await scraper.GetClubOverviewAsync(request.TeamId, cancellationToken).ConfigureAwait(false);
        return ClubOverviewDto.From(overview);
    }
}
