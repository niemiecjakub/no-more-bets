using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Application.Matches.GetMatchesReadyForPrediction;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.SemanticSearchMatches;

public record SemanticSearchMatchesQuery(string Query) : IRequest<IReadOnlyList<MatchDto>>;

public sealed class SemanticSearchMatchesHandler(
  IEmbeddingService embeddingService,
  IDocumentChunkSearch documentChunkSearch,
  IUnitOfWork unitOfWork,
  IMediator mediator) : IRequestHandler<SemanticSearchMatchesQuery, IReadOnlyList<MatchDto>>
{
  public async Task<IReadOnlyList<MatchDto>> Handle(
    SemanticSearchMatchesQuery request,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Query))
      return [];

    var embedding = await embeddingService
      .EmbedAsync(request.Query.Trim(), cancellationToken)
      .ConfigureAwait(false);

    var rankedMatchIds = await documentChunkSearch
      .FindMatchIdsAsync(request.Query.Trim(), embedding, embeddingService.ModelId, cancellationToken)
      .ConfigureAwait(false);

    if (rankedMatchIds.Count == 0)
      return [];

    var matches = await unitOfWork.Matches
      .GetMatchesByIdsAsync(rankedMatchIds, cancellationToken)
      .ConfigureAwait(false);

    var byId = matches.ToDictionary(m => m.Id);
    var ordered = rankedMatchIds
      .Where(byId.ContainsKey)
      .Select(id => byId[id])
      .ToList();

    if (ordered.Count == 0)
      return [];

    var readyForPrediction = await mediator
      .Send(new GetUpcomingMatchesReadyForPredictionQuery(ExcludeWithExistingResearch: false), cancellationToken)
      .ConfigureAwait(false);
    var completeSet = readyForPrediction.Select(m => m.Id).ToHashSet();

    var pageIds = ordered.Select(m => m.Id).ToList();

    var hasLineupSet = await unitOfWork.Matches
      .GetMatchIdsWithLineupAsync(pageIds, cancellationToken)
      .ConfigureAwait(false);
    var hasOddsSet = await unitOfWork.Matches
      .GetMatchIdsWithOddsAsync(pageIds, cancellationToken)
      .ConfigureAwait(false);
    var hasHeadToHeadSet = await unitOfWork.Matches
      .GetMatchIdsWithHeadToHeadAsync(pageIds, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchSet = await unitOfWork.Matches
      .GetMatchIdsWithAnalysisCodeAsync(pageIds, MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);
    var hasResearchBetSet = await unitOfWork.Betting
      .GetMatchIdsWithResearchPhaseSelectionsAsync(pageIds, cancellationToken)
      .ConfigureAwait(false);

    return ordered
      .Select(m => MatchDtoMapper.MapToMatchDto(
        m,
        completeSet,
        hasResearchSet,
        hasResearchBetSet,
        hasLineupSet,
        hasOddsSet,
        hasHeadToHeadSet))
      .ToList();
  }
}
