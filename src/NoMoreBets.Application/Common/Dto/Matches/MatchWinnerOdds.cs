namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match winner odds.</summary>
public record MatchWinnerOdds
{
    public double? Home { get; init; }
    public double? Draw { get; init; }
    public double? Away { get; init; }
}
