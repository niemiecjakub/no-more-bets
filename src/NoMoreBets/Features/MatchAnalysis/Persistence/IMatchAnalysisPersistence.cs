using NoMoreBets.Features.MatchAnalysis.Model;

namespace NoMoreBets.Features.MatchAnalysis.Persistence;

/// <summary>
/// Persists match analysis results (e.g. to file).
/// </summary>
public interface IMatchAnalysisPersistence
{
    /// <summary>Saves match analysis results and returns the path written.</summary>
    Task<string> SaveResultsAsync(IReadOnlyList<Model.MatchAnalysis> results, CancellationToken cancellationToken = default);
}
