namespace NoMoreBets.Domain.Matches;

public readonly record struct MatchResultOdds(decimal? Home, decimal? Draw, decimal? Away);
