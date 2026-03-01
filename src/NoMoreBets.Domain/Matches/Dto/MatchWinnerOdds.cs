namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Match winner odds.</summary>
public record MatchWinnerOdds
{
    public double? Home { get; init; }
    public double? Draw { get; init; }
    public double? Away { get; init; }
}
