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
    var utcNow = DateTime.UtcNow;
    var kickoffWithinTwoDaysEnd = utcNow.AddDays(2);

    var dataComplete = request.ExcludeWithExistingResearch
      ? await unitOfWork.Matches
          .GetUpcomingReadyForPredictionWithoutResearchAnalysisAsync(cancellationToken)
          .ConfigureAwait(false)
      : await unitOfWork.Matches
          .GetUpcomingMatchesReadyForPredictionAsync(cancellationToken)
          .ConfigureAwait(false);

    var allUpcoming = await unitOfWork.Matches
      .GetUpcomingMatchesAsync(cancellationToken)
      .ConfigureAwait(false);

    var soonKickoff = allUpcoming
      .Where(m => m.MatchDate > utcNow && m.MatchDate <= kickoffWithinTwoDaysEnd)
      .ToList();

    if (request.ExcludeWithExistingResearch)
    {
      var dataCompleteIds = dataComplete.Select(m => m.Id).ToHashSet();
      var soonFiltered = new List<Match>(soonKickoff.Count);
      foreach (var m in soonKickoff)
      {
        if (dataCompleteIds.Contains(m.Id))
          continue;

        var research = await unitOfWork.Matches
          .GetLatestMatchAnalysisByCodeAsync(m.Id, MatchAnalysis.ResearchCode, cancellationToken)
          .ConfigureAwait(false);
        if (research is null)
          soonFiltered.Add(m);
      }

      soonKickoff = soonFiltered;
    }

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
}
