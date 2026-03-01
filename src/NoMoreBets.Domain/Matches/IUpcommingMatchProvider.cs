using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Domain.Matches;
public interface IUpcommingMatchProvider
{
  public Task<IReadOnlyList<LeagueMatchPreviews>> GetMatchPreviewsUpcomingAsync(int? soccerdataLeagueId = null, CancellationToken cancellationToken = default);
}
