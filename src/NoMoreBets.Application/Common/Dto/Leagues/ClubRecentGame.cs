namespace NoMoreBets.Application.Common.Dto.Leagues;

/// <summary>One recent game from a club's form (opponent, score, result, match link).</summary>
public class ClubRecentGame
{
    public int OpponentId { get; init; }
    public string Score { get; init; } = "";
    public MatchResult Result { get; init; }
    public string GameUrl { get; init; } = "";
}
