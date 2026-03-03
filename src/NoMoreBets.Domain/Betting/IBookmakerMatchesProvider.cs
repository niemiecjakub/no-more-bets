using NoMoreBets.Domain.Betting.Dto;

namespace NoMoreBets.Domain.Betting;
public interface IBookmakerMatchesProvider
{
  Task<IReadOnlyList<UpcomingGame>> GetUpcomingGamesAsync(CancellationToken cancellationToken);
}
