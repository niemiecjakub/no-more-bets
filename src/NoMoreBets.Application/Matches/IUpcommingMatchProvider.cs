using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Matches;

public interface IUpcommingMatchProvider
{
  public Task<IReadOnlyList<LeagueMatchPreviews>> GetMatchPreviewsUpcomingAsync(int? soccerdataLeagueId = null, CancellationToken cancellationToken = default);
}
