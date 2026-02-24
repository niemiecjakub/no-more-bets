using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;

/// <summary>
/// Query to fetch match details (general info and lineups) from a FotMob match detail page.
/// </summary>
/// <param name="GameUrl">FotMob match page URL.</param>
public record GetFotmobMatchDetailsQuery(string GameUrl) : IRequest<MatchDetailsDto>;

/// <summary>
/// Handles <see cref="GetFotmobMatchDetailsQuery"/> by delegating to <see cref="FotmobScraper"/>.
/// </summary>
public class GetFotmobMatchDetailsHandler(FotmobScraper scraper) : IRequestHandler<GetFotmobMatchDetailsQuery, MatchDetailsDto>
{
    /// <inheritdoc />
    public async Task<MatchDetailsDto> Handle(GetFotmobMatchDetailsQuery request, CancellationToken cancellationToken)
    {
        var details = await scraper.GetMatchDetailsAsync(request.GameUrl, cancellationToken).ConfigureAwait(false);
        return MatchDetailsDto.From(details);
    }
}
