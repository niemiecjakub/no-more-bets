namespace NoMoreBets.Application.Betting.Common;

public record BetSelectionItemDto(
  int MatchId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  string EventTypeName,
  string EventOptionName,
  decimal OddsAtPlacement,
  int StatusId,
  string StatusName);
