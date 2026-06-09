namespace NoMoreBets.Application.Matches.GetMatchesPage;

public record MatchDto(
  int Id,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string LeagueName,
  string LeagueSlug,
  int MatchStatusId,
  string MatchStatusName,
  int? HomeGoals,
  int? AwayGoals,
  string? BetclicUrl,
  bool IsReadyToPredict = false,
  bool HasResearch = false,
  bool HasResearchBet = false,
  bool HasLineup = false,
  bool HasOdds = false,
  bool HasHeadToHead = false);
