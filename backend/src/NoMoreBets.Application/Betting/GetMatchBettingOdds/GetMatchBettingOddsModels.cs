namespace NoMoreBets.Application.Betting.GetMatchBettingOdds;

public record CurrentOddsMarket(int EventTypeId, string EventTypeName, IReadOnlyList<CurrentOddsOption> Options);

public record CurrentOddsOption(string Label, double Odds);
