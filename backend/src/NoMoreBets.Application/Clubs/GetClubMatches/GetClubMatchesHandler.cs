using MediatR;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;

namespace NoMoreBets.Application.Clubs.GetClubMatches;

public record GetClubMatchesQuery(int ClubId) : IRequest<IReadOnlyList<MatchDto>?>;

public sealed class GetClubMatchesHandler(IBettingRepository betting, IMatchRepository matches, IClubRepository clubs, IMediator mediator)
  : IRequestHandler<GetClubMatchesQuery, IReadOnlyList<MatchDto>?>
{
  public async Task<IReadOnlyList<MatchDto>?> Handle(
    GetClubMatchesQuery request,
    CancellationToken cancellationToken)
  {
    var club = await clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    var clubMatches = await matches
      .GetMatchesForClubAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (clubMatches.Count == 0)
      return Array.Empty<MatchDto>();

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);
    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    var matchIds = clubMatches.Select(m => m.Id).ToList();

    var hasLineupSet = await matches
      .GetMatchIdsWithLineupAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var oddsByMatch = await matches
      .GetLatestMatchResultOddsAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var hasHeadToHeadSet = await matches
      .GetMatchIdsWithHeadToHeadAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchSet = await matches
      .GetMatchIdsWithAnalysisCodeAsync(matchIds, Domain.Matches.MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchBetSet = await betting
      .GetMatchIdsWithResearchPhaseSelectionsAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);

    return clubMatches
      .Select(m => MatchDtoMapper.MapToMatchDto(
        m,
        completeSet,
        hasResearchSet,
        hasResearchBetSet,
        hasLineupSet,
        hasHeadToHeadSet,
        oddsByMatch))
      .ToList();
  }
}
