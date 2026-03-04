using NoMoreBets.Application.Common.Dto.Betting;

namespace NoMoreBets.Application.Betting;

public interface IBookmakerMatchesProvider
{
  Task<IReadOnlyList<UpcomingGame>> GetUpcomingGamesAsync(CancellationToken cancellationToken);
}
