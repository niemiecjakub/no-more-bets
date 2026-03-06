using MediatR;
using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Clubs.GetOverview;

public record GetClubOverviewQuery(int FotmobClubId) : IRequest<ClubOverview>;

public class GetClubOverviewHandler(IClubOverviewProvider clubOverviewProvider) : IRequestHandler<GetClubOverviewQuery, ClubOverview>
{
  /// <inheritdoc />
  public async Task<ClubOverview> Handle(GetClubOverviewQuery request, CancellationToken cancellationToken)
  {
    return await clubOverviewProvider.GetClubOverviewAsync(request.FotmobClubId, cancellationToken).ConfigureAwait(false);
  }
}
