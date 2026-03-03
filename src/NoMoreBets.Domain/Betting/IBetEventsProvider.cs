using NoMoreBets.Domain.Betting.Dto;

namespace NoMoreBets.Domain.Betting;
public interface IBetEventsProvider
{
  public Task<IReadOnlyList<BookmakerEvent>> GetMatchEventsAsync(string gameUrl, bool expand, CancellationToken cancellationToken = default);
}
