namespace NoMoreBets.Domain.Matches;

public sealed record MatchPage(IReadOnlyList<Match> Items, bool HasMore);
