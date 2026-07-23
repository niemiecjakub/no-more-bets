using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;

/// <summary>
/// Upcoming matches ready for prediction: upcoming status, a betting odds snapshot, and kickoff
/// (UTC) in the future within two days from now.
/// </summary>
/// <param name="ExcludeWithExistingResearch">
/// When true (default), matches that already have agent research analysis are omitted
/// (Hangfire research scheduling). When false, every soon-kickoff fixture is included.
/// </param>
public record GetUpcomingMatchesReadyForPredictionQuery(bool ExcludeWithExistingResearch = true) : IRequest<IReadOnlyList<Match>>;

public sealed class GetUpcomingMatchesReadyForPredictionHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUpcomingMatchesReadyForPredictionQuery, IReadOnlyList<Match>>
{
  public async Task<IReadOnlyList<Match>> Handle(
    GetUpcomingMatchesReadyForPredictionQuery request,
    CancellationToken cancellationToken)
  {
    var utcNow = DateTime.UtcNow;
    var kickoffWithinTwoDaysEnd = utcNow.AddDays(2);

    var upcomingWithOdds = await unitOfWork.Matches
      .GetUpcomingMatchesWithOddsSnapshotsAsync(cancellationToken)
      .ConfigureAwait(false);

    var soonKickoff = upcomingWithOdds
      .Where(m => m.MatchDate > utcNow && m.MatchDate <= kickoffWithinTwoDaysEnd)
      .OrderBy(m => m.MatchDate)
      .ToList();

    if (soonKickoff.Count == 0 || !request.ExcludeWithExistingResearch)
      return soonKickoff;

    var soonKickoffIds = soonKickoff.Select(m => m.Id).ToArray();
    var researchedMatchIds = await unitOfWork.Matches
      .GetMatchIdsWithAnalysisCodeAsync(soonKickoffIds, MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);

    return soonKickoff
      .Where(m => !researchedMatchIds.Contains(m.Id))
      .ToList();
  }
}
