namespace NoMoreBets.Application.Clubs.GetClubById;

public record ClubDetailDto(
  int Id,
  string Name,
  int LeagueId,
  string LeagueName,
  string Slug,
  string LeagueSlug);
