namespace NoMoreBets.Application.Clubs.GetClubsList;

public record ClubDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);
