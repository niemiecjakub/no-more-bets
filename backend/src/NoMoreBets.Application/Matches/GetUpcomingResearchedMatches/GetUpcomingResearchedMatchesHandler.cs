using MediatR;
using NoMoreBets.Application.Matches.GetMatchesPage;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetUpcomingResearchedMatches;

public record GetUpcomingResearchedMatchesQuery : IRequest<IReadOnlyList<MatchDto>>;

public sealed class GetUpcomingResearchedMatchesHandler(IMatchRepository matches)
  : IRequestHandler<GetUpcomingResearchedMatchesQuery, IReadOnlyList<MatchDto>>
{
  public async Task<IReadOnlyList<MatchDto>> Handle(
    GetUpcomingResearchedMatchesQuery _,
    CancellationToken cancellationToken)
  {
    var upcoming = await matches
      .GetUpcomingMatchesWithAnalysisCodeAsync(MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);

    if (upcoming.Count == 0)
      return Array.Empty<MatchDto>();

    var hasResearchSet = upcoming.Select(m => m.Id).ToHashSet();
    var emptySet = new HashSet<int>();

    return upcoming
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
