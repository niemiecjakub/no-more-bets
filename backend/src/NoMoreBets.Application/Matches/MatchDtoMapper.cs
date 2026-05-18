using NoMoreBets.Application.Matches.Dto;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Matches;

public static class MatchDtoMapper
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

  public static MatchDto MapToMatchDto(
    Match m,
    IReadOnlySet<int> completeSet,
    IReadOnlySet<int> hasResearchSet,
    IReadOnlySet<int> hasResearchBetSet,
    IReadOnlySet<int> hasLineupSet,
    IReadOnlySet<int> hasOddsSet,
    IReadOnlySet<int> hasHeadToHeadSet) =>
    new(
      m.Id,
      m.MatchDate,
      m.HomeClubId,
      m.AwayClubId,
      m.HomeClub.Name,
      m.AwayClub.Name,
      m.HomeClub.Slug,
      m.AwayClub.Slug,
      m.Stage == null ? string.Empty : m.Stage.Season.League.Name,
      m.Stage == null ? string.Empty : m.Stage.Season.League.Slug,
      m.MatchStatusId,
      m.MatchStatusEntity.Name,
      m.HomeGoals,
      m.AwayGoals,
      m.BetclicUrl,
      completeSet.Contains(m.Id),
      hasResearchSet.Contains(m.Id),
      hasResearchBetSet.Contains(m.Id),
      hasLineupSet.Contains(m.Id),
      hasOddsSet.Contains(m.Id),
      hasHeadToHeadSet.Contains(m.Id));
}
