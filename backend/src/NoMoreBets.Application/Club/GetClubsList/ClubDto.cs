using NoMoreBets.Application.Common.Dto.Clubs;

namespace NoMoreBets.Application.Clubs.GetClubsList;

public record ClubDto(
  int Id,
  string Name,
  string Slug,
  IReadOnlyList<ClubSeasonMembershipDto> Memberships);
