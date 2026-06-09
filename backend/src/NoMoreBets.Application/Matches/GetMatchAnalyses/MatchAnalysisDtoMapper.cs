using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Matches.GetMatchAnalyses;

public static class MatchAnalysisDtoMapper
{
  public static StructuredMatchAnalysisDto? MapStructured(StructuredMatchAnalysis? analysis) =>
    analysis == null
      ? null
      : new StructuredMatchAnalysisDto(
        analysis.Context,
        analysis.Form,
        analysis.Tactics,
        analysis.Squad,
        analysis.Statistics,
        analysis.Market,
        analysis.MatchProjection,
        analysis.Prediction);
}
