using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchesPage;

public record GetMatchesPageQuery(
  int Limit,
  int? MatchStatusId,
  IReadOnlyList<int> LeagueIds,
  DateTime? AfterMatchDateUtc,
  int? AfterId) : IRequest<PagedResponse<MatchDto>>;

public sealed class GetMatchesPageHandler(IUnitOfWork unitOfWork, IMediator mediator)
  : IRequestHandler<GetMatchesPageQuery, PagedResponse<MatchDto>>
{
  public async Task<PagedResponse<MatchDto>> Handle(
    GetMatchesPageQuery request,
    CancellationToken cancellationToken)
  {
    var page = await unitOfWork.Matches
      .GetMatchesPageAsync(
        request.Limit,
        request.MatchStatusId,
        request.LeagueIds,
        request.AfterMatchDateUtc,
        request.AfterId,
        cancellationToken)
      .ConfigureAwait(false);

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);
    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    var pageIds = page.Items.Select(m => m.Id).ToList();

    IReadOnlySet<int> hasLineupSet = new HashSet<int>();
    IReadOnlySet<int> hasOddsSet = new HashSet<int>();
    IReadOnlySet<int> hasHeadToHeadSet = new HashSet<int>();
    IReadOnlySet<int> hasResearchSet = new HashSet<int>();
    IReadOnlySet<int> hasResearchBetSet = new HashSet<int>();

    if (pageIds.Count > 0)
    {
      hasLineupSet = await unitOfWork.Matches
        .GetMatchIdsWithLineupAsync(pageIds, cancellationToken)
        .ConfigureAwait(false);
      hasOddsSet = await unitOfWork.Matches
        .GetMatchIdsWithOddsAsync(pageIds, cancellationToken)
        .ConfigureAwait(false);
      hasHeadToHeadSet = await unitOfWork.Matches
        .GetMatchIdsWithHeadToHeadAsync(pageIds, cancellationToken)
        .ConfigureAwait(false);
      hasResearchSet = await unitOfWork.Matches
        .GetMatchIdsWithAnalysisCodeAsync(pageIds, MatchAnalysis.ResearchCode, cancellationToken)
        .ConfigureAwait(false);
      hasResearchBetSet = await unitOfWork.Betting
        .GetMatchIdsWithResearchPhaseSelectionsAsync(pageIds, cancellationToken)
        .ConfigureAwait(false);
    }

    var items = page.Items
      .Select(m => MatchDtoMapper.MapToMatchDto(
        m,
        completeSet,
        hasResearchSet,
        hasResearchBetSet,
        hasLineupSet,
        hasOddsSet,
        hasHeadToHeadSet))
      .ToList();

    return PagedResponseFactory.Create(items, page.HasMore, item => item.MatchDate, item => item.Id);
  }
}
