using NoMoreBets.Application.Common.Dto.Clubs;

namespace NoMoreBets.Application.Clubs.GetClubById;

public record ClubDetailDto(
  int Id,
  string Name,
  string Slug,
  IReadOnlyList<ClubSeasonMembershipDto> Memberships);
