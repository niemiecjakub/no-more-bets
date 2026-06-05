using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;

public record Player(string Name, string Position);

public record AgentTeamLineup(IReadOnlyList<Player> Players);

public record AgentMatchLineup(AgentTeamLineup Home, AgentTeamLineup Away);

public record CurrentOddsMarket(int EventTypeId, string EventTypeName, IReadOnlyList<CurrentOddsOption> Options);

public record CurrentOddsOption(string Label, double Odds);

public record MatchEventMarket(int EventTypeId, string EventTypeName, IReadOnlyList<string> Options);

[Description("Match available for betting: use Id when calling GetCurrentOdds and GetMatchAnalysis")]
public record AvailableMatch(int Id, string HomeClubName, string AwayClubName, DateTime MatchDate);
