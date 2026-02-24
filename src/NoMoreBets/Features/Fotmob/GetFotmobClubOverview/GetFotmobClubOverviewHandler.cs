using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobClubOverview;

/// <summary>
/// Query to fetch club overview (recent games and daily summary) from FotMob team overview page.
/// </summary>
public record GetFotmobClubOverviewQuery(int TeamId) : IRequest<ClubOverviewDto>;

/// <summary>
/// Handles <see cref="GetFotmobClubOverviewQuery"/> by delegating to <see cref="FotmobScraper"/>.
/// </summary>
public class GetFotmobClubOverviewHandler(FotmobScraper scraper) : IRequestHandler<GetFotmobClubOverviewQuery, ClubOverviewDto>
{
    /// <inheritdoc />
    public async Task<ClubOverviewDto> Handle(GetFotmobClubOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await scraper.GetClubOverviewAsync(request.TeamId, cancellationToken).ConfigureAwait(false);
        return ClubOverviewDto.From(overview);
    }
}
