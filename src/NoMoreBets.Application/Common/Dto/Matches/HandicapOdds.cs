namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Handicap odds.</summary>
public record HandicapOdds
{
    public double? Market { get; init; }
    public double? Home { get; init; }
    public double? Away { get; init; }
}
