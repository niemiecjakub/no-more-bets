using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetUpcomingResearchedMatches;

public record GetUpcomingResearchedMatchesQuery : IRequest<IReadOnlyList<MatchDto>>;

public sealed class GetUpcomingResearchedMatchesHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetUpcomingResearchedMatchesQuery, IReadOnlyList<MatchDto>>
{
  public async Task<IReadOnlyList<MatchDto>> Handle(
    GetUpcomingResearchedMatchesQuery _,
    CancellationToken cancellationToken)
  {
    var matches = await unitOfWork.Matches
      .GetUpcomingMatchesWithAnalysisCodeAsync(MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);

    if (matches.Count == 0)
      return Array.Empty<MatchDto>();

    var hasResearchSet = matches.Select(m => m.Id).ToHashSet();
    var emptySet = new HashSet<int>();

    return matches
      .Select(m => MatchDtoMapper.MapToMatchDto(
        m,
        emptySet,
        hasResearchSet,
        emptySet,
        emptySet,
        emptySet,
        new Dictionary<int, MatchResultOdds>()))
      .ToList();
  }
}
