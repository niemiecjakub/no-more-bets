using NoMoreBets.Application.Common.Dto.Betting;

namespace NoMoreBets.Application.Betting;

public interface IBetEventsProvider
{
  public Task<IReadOnlyList<BookmakerEvent>> GetMatchEventsAsync(string gameUrl, bool expand, CancellationToken cancellationToken = default);
}
