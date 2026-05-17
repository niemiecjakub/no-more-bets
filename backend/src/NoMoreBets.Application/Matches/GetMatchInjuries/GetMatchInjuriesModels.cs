namespace NoMoreBets.Application.Matches.GetMatchInjuries;

public record InjuriedPlayer(string Name, string Position, string InjuryStatus);

public record TeamInjuriesResult(IReadOnlyList<InjuriedPlayer> Injuries);

public record MatchInjuriesResult(TeamInjuriesResult Home, TeamInjuriesResult Away);
