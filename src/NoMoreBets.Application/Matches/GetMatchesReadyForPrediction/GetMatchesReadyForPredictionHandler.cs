using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;

/// <summary>
/// Upcoming matches ready for prediction: those with preview, lineup, odds snapshot, and head-to-head data,
/// merged with upcoming matches whose kickoff (UTC) is in the future and within two days from now.
/// </summary>
/// <param name="ExcludeWithExistingResearch">
/// When true (default), matches that already have agent research analysis are omitted from both sources
/// (Hangfire research scheduling). When false, the data-complete set includes all such matches; the soon-kickoff
/// set still includes every soon fixture regardless of research.
/// </param>
public record GetUpcomingMatchesReadyForPredictionQuery(bool ExcludeWithExistingResearch = true) : IRequest<IReadOnlyList<Match>>;

public sealed class GetUpcomingMatchesReadyForPredictionHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUpcomingMatchesReadyForPredictionQuery, IReadOnlyList<Match>>
{
  public async Task<IReadOnlyList<Match>> Handle(
    GetUpcomingMatchesReadyForPredictionQuery request,
    CancellationToken cancellationToken)
  {
    var dataComplete = await GetDataCompleteMatchesAsync(request.ExcludeWithExistingResearch, cancellationToken)
      .ConfigureAwait(false);
    var soonKickoff = await GetSoonKickoffMatchesWithOddsAsync(
        request.ExcludeWithExistingResearch,
        cancellationToken)
      .ConfigureAwait(false);

    var byId = new Dictionary<int, Match>();
    foreach (var m in dataComplete)
      byId[m.Id] = m;
    foreach (var m in soonKickoff)
    {
      if (!byId.ContainsKey(m.Id))
        byId[m.Id] = m;
    }

    return byId.Values.OrderBy(m => m.MatchDate).ToList();
  }

  private async Task<IReadOnlyList<Match>> GetDataCompleteMatchesAsync(
    bool excludeWithExistingResearch,
    CancellationToken cancellationToken)
  {
    return excludeWithExistingResearch
      ? await unitOfWork.Matches
          .GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(cancellationToken)
          .ConfigureAwait(false)
      : await unitOfWork.Matches
          .GetUpcomingMatchesReadyForPredictionAsync(cancellationToken)
          .ConfigureAwait(false);
  }

  private async Task<IReadOnlyList<Match>> GetSoonKickoffMatchesWithOddsAsync(
    bool excludeWithExistingResearch,
    CancellationToken cancellationToken)
  {
    var utcNow = DateTime.UtcNow;
    var kickoffWithinTwoDaysEnd = utcNow.AddDays(2);

    var upcomingWithOdds = await unitOfWork.Matches
      .GetUpcomingMatchesWithOddsSnapshotsAsync(cancellationToken)
      .ConfigureAwait(false);

    var soonKickoff = upcomingWithOdds
      .Where(m => m.MatchDate > utcNow && m.MatchDate <= kickoffWithinTwoDaysEnd)
      .ToList();

    if (soonKickoff.Count == 0)
      return soonKickoff;

    if (!excludeWithExistingResearch)
      return soonKickoff;

    var researchChecks = soonKickoff.Select(async m => new
    {
      Match = m,
      Research = await unitOfWork.Matches
        .GetLatestMatchAnalysisByCodeAsync(m.Id, MatchAnalysis.ResearchCode, cancellationToken)
        .ConfigureAwait(false)
    });

    return (await Task.WhenAll(researchChecks).ConfigureAwait(false))
      .Where(x => x.Research is null)
      .Select(x => x.Match)
      .ToList();
  }
}
