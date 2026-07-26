using NoMoreBets.Application.Common.Dto.Matches;

namespace NoMoreBets.Application.Matches;

/// <summary>Provides finished match results (scores) for a league slug from an external source.</summary>
public interface IMatchResultsProvider
{
  Task<IReadOnlyList<FinishedMatchResult>> GetFinishedResultsAsync(
    string leagueSlug,
    CancellationToken cancellationToken = default);
}
