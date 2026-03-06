using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Matches;

/// <summary>Provides match details (e.g. from FotMob) by game URL.</summary>
public interface IMatchDetailsProvider
{
  Task<MatchDetailsDto> GetMatchDetailsAsync(string gameUrl, CancellationToken cancellationToken = default);
}
