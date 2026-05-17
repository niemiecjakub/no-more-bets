namespace NoMoreBets.Application.Matches.GetMatchLineups;

public record TeamLineupResult(string LineupType, IReadOnlyList<Player> Players);

public record Player(string Name, string Position);

public record MatchLineupResult(TeamLineupResult Home, TeamLineupResult Away);
