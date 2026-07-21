using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchAnalyses;

public record GetMatchAnalysesQuery(int MatchId) : IRequest<MatchAnalysisPageDto?>;

public sealed class GetMatchAnalysesHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchAnalysesQuery, MatchAnalysisPageDto?>
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
      .GetLatestMatchAnalysisByCodeAsync(request.MatchId, MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);

    researchAnalysis ??= await unitOfWork.Matches
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
        MatchAnalysisDtoMapper.MapStructured(a.TryGetStructuredAnalysis())))
      .ToList();

    var league = match.Stage?.Season.League;
    var seasonYear = match.Stage?.Season.Year ?? string.Empty;

    return new MatchAnalysisPageDto(
      match.Id,
      match.HomeClubId,
      match.AwayClubId,
      match.HomeClub.Name,
      match.AwayClub.Name,
      match.HomeClub.Slug,
      match.AwayClub.Slug,
      league?.Name ?? string.Empty,
      league?.Slug ?? string.Empty,
      seasonYear,
      match.MatchStatusId,
      match.HomeGoals,
      match.AwayGoals,
      match.MatchDate,
      analyses,
      researchAnalysis?.AgentSessionId);
  }
}
