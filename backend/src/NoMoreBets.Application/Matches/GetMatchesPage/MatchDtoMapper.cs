using NoMoreBets.Application.Common.Dto.Matches;
using DomainMatch = NoMoreBets.Domain.Matches.Match;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchesPage;

public static class MatchDtoMapper
{
  public static MatchDto MapToMatchDto(
    DomainMatch m,
    IReadOnlySet<int> completeSet,
    IReadOnlySet<int> hasResearchSet,
    IReadOnlySet<int> hasResearchBetSet,
    IReadOnlySet<int> hasLineupSet,
    IReadOnlySet<int> hasHeadToHeadSet,
    IReadOnlyDictionary<int, MatchResultOdds> oddsByMatch) =>
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
      hasHeadToHeadSet.Contains(m.Id),
      MapOdds(oddsByMatch, m.Id));

  private static MatchWinnerOdds? MapOdds(
    IReadOnlyDictionary<int, MatchResultOdds> oddsByMatch,
    int matchId)
  {
    if (!oddsByMatch.TryGetValue(matchId, out var odds))
      return null;

    return new MatchWinnerOdds
    {
      Home = odds.Home is null ? null : (double)odds.Home.Value,
      Draw = odds.Draw is null ? null : (double)odds.Draw.Value,
      Away = odds.Away is null ? null : (double)odds.Away.Value,
    };
  }
}
