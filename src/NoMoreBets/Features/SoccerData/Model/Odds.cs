namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Match odds.</summary>
public record Odds
{
    public MatchWinnerOdds MatchWinner { get; init; } = null!;
    public OverUnderOdds OverUnder { get; init; } = null!;
    public HandicapOdds Handicap { get; init; } = null!;
    public int? LastModifiedTimestamp { get; init; }
}
