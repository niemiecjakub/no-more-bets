using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Domain.Leagues;

public record LeagueTableStanding(int ClubId, string ClubName, ClubLeagueStats Stats);
