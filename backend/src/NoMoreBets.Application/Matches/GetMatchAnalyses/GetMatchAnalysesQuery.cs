using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.Dto;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchAnalyses;

public record GetMatchAnalysesQuery(int MatchId) : IRequest<MatchAnalysisPageDto?>;

public sealed class GetMatchAnalysesHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetMatchAnalysesQuery, MatchAnalysisPageDto?>
{
  public async Task<MatchAnalysisPageDto?> Handle(
    GetMatchAnalysesQuery request,
    CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    if (match == null)
      return null;

    var researchAnalysis = await unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(request.MatchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);

    var analysisEntities = await unitOfWork.Matches
      .GetNonResearchAnalysesForMatchAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);

    var analyses = analysisEntities
      .Select(a => new MatchAnalysisItemDto(
        a.Id,
        a.Code,
        a.Content,
        MatchDtoMapper.MapStructured(a.GetAnalysis())))
      .ToList();

    return new MatchAnalysisPageDto(
      match.Id,
      match.HomeClub.Name,
      match.AwayClub.Name,
      match.HomeClub.Slug,
      match.AwayClub.Slug,
      match.MatchStatusId,
      match.HomeGoals,
      match.AwayGoals,
      match.MatchDate,
      analyses,
      researchAnalysis?.AgentSessionId);
  }
}
