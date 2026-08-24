using System.Text.Json.Serialization;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubNextMatch;

public record ClubNextMatchDto(
  int MatchId,
  [property: JsonConverter(typeof(WallClockDateTime.JsonConverter))]
  DateTime MatchDate,
  int HomeClubId,
  int AwayClubId,
  string HomeClubName,
  string AwayClubName,
  string HomeClubSlug,
  string AwayClubSlug,
  bool IsHome);
