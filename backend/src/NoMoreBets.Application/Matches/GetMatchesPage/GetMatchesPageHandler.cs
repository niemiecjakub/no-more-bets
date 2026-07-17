using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchesPage;

public record GetMatchesPageQuery(
  int Limit,
  int? MatchStatusId,
  IReadOnlyList<int> LeagueIds,
  DateTime? AfterMatchDateUtc,
  int? AfterId,
  MatchDateSortOrder SortOrder = MatchDateSortOrder.Descending,
  string? Search = null) : IRequest<Paged<MatchDto>>;

public sealed class GetMatchesPageHandler(
  IUnitOfWork unitOfWork,
  IMediator mediator,
  IEmbeddingService embeddingService,
  IDocumentChunkSearch documentChunkSearch)
  : IRequestHandler<GetMatchesPageQuery, Paged<MatchDto>>
{
  public async Task<Paged<MatchDto>> Handle(
    GetMatchesPageQuery request,
    CancellationToken cancellationToken)
  {
    MatchPage page;
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
      page = await GetHybridSearchPageAsync(request, cancellationToken).ConfigureAwait(false);
    }
    else
    {
      page = await unitOfWork.Matches
        .GetMatchesPageAsync(
          request.Limit,
          request.MatchStatusId,
          request.LeagueIds,
          request.AfterMatchDateUtc,
          request.AfterId,
          request.SortOrder,
          cancellationToken)
        .ConfigureAwait(false);
    }

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
        .GetMatchIdsWithAnalysisCodeAsync(pageIds, MatchAnalysis.StructuredResearchCode, cancellationToken)
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

    return PagedFactory.Create(items, page.HasMore, item => item.MatchDate, item => item.Id);
  }

  private async Task<MatchPage> GetHybridSearchPageAsync(
    GetMatchesPageQuery request,
    CancellationToken cancellationToken)
  {
    var query = request.Search!.Trim();
    var embedding = await embeddingService
      .EmbedAsync(query, cancellationToken)
      .ConfigureAwait(false);

    var rankedMatchIds = await documentChunkSearch
      .FindMatchIdsAsync(query, embedding, embeddingService.ModelId, cancellationToken)
      .ConfigureAwait(false);

    if (rankedMatchIds.Count == 0)
      return new MatchPage([], false);

    var matches = await unitOfWork.Matches
      .GetMatchesByIdsAsync(rankedMatchIds, cancellationToken)
      .ConfigureAwait(false);

    var byId = matches.ToDictionary(m => m.Id);
    var selectedLeagueIds = request.LeagueIds.Distinct().ToHashSet();
    var hasLeagueFilter = selectedLeagueIds.Count > 0;

    var ordered = rankedMatchIds
      .Where(byId.ContainsKey)
      .Select(id => byId[id])
      .Where(m => !request.MatchStatusId.HasValue || m.MatchStatusId == request.MatchStatusId.Value)
      .Where(m => !hasLeagueFilter
        || (m.Stage != null && selectedLeagueIds.Contains(m.Stage.Season.LeagueId)))
      .ToList();

    if (request.AfterId is int afterId)
    {
      var cursorIndex = ordered.FindIndex(m => m.Id == afterId);
      if (cursorIndex >= 0)
        ordered = ordered.Skip(cursorIndex + 1).ToList();
    }

    var hasMore = ordered.Count > request.Limit;
    if (hasMore)
      ordered = ordered.Take(request.Limit).ToList();

    return new MatchPage(ordered, hasMore);
  }
}
