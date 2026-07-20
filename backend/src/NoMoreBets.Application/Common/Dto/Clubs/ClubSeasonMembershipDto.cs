namespace NoMoreBets.Application.Common.Dto.Clubs;

public record ClubSeasonMembershipDto(
  int SeasonId,
  string SeasonYear,
  DateOnly? StartDate,
  DateOnly? EndDate,
  int LeagueId,
  string LeagueName,
  string LeagueSlug);
