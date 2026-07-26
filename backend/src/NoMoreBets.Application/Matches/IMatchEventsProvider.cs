using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Matches;

/// <summary>Provides match incident events from an external source for a single match detail page.</summary>
public interface IMatchEventsProvider
{
  Task<IReadOnlyList<MatchEvent>> GetMatchEventsAsync(
    string matchDetailUrl,
    CancellationToken cancellationToken = default);
}
