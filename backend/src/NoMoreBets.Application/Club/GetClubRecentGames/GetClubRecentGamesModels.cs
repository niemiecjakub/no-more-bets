namespace NoMoreBets.Application.Clubs.GetClubRecentGames;

public record RecentMatch(int MatchId, string Opponent, string Score, string Result, DateOnly Date);
