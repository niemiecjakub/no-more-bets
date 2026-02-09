using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobMatchDetails.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;

namespace NoMoreBets.Features.Fotmob.GetFotmobMatchDetails;

/// <summary>
/// Handles <see cref="GetFotmobMatchDetailsQuery"/> by delegating to <see cref="IFotmobScraper"/>.
/// </summary>
public class GetFotmobMatchDetailsHandler(IFotmobScraper scraper) : IRequestHandler<GetFotmobMatchDetailsQuery, MatchDetailsDto>
{
    /// <inheritdoc />
    public async Task<MatchDetailsDto> Handle(GetFotmobMatchDetailsQuery request, CancellationToken cancellationToken)
    {
        var details = await scraper.GetMatchDetailsAsync(request.GameUrl, cancellationToken).ConfigureAwait(false);
        return MatchDetailsDto.From(details);
    }
}
