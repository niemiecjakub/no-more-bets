using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;

namespace NoMoreBets.Application.Clubs.GetClubMatches;

public record GetClubMatchesQuery(int ClubId) : IRequest<IReadOnlyList<MatchDto>?>;

public sealed class GetClubMatchesHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetClubMatchesQuery, IReadOnlyList<MatchDto>?>
{
  public async Task<IReadOnlyList<MatchDto>?> Handle(
    GetClubMatchesQuery request,
    CancellationToken cancellationToken)
  {
    var club = await unitOfWork.Clubs
      .GetByIdAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (club == null)
      return null;

    var matches = await unitOfWork.Matches
      .GetMatchesForClubAsync(request.ClubId, cancellationToken)
      .ConfigureAwait(false);

    if (matches.Count == 0)
      return Array.Empty<MatchDto>();

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);
    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    var matchIds = matches.Select(m => m.Id).ToList();

    var hasLineupSet = await unitOfWork.Matches
      .GetMatchIdsWithLineupAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var oddsByMatch = await unitOfWork.Matches
      .GetLatestMatchResultOddsAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var hasHeadToHeadSet = await unitOfWork.Matches
      .GetMatchIdsWithHeadToHeadAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchSet = await unitOfWork.Matches
      .GetMatchIdsWithAnalysisCodeAsync(matchIds, Domain.Matches.MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchBetSet = await unitOfWork.Betting
      .GetMatchIdsWithResearchPhaseSelectionsAsync(matchIds, cancellationToken)
      .ConfigureAwait(false);

    return matches
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
