namespace NoMoreBets.Application.Clubs.GetClubNextMatch;

public record ClubNextMatchDto(
  int MatchId,
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  bool IsHome);
