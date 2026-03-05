using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Clubs;

public interface IClubOverviewProvider
{
  Task<ClubOverview> GetClubOverviewAsync(int teamId, CancellationToken cancellationToken = default);
}
