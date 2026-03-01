namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Over/under odds.</summary>
public record OverUnderOdds
{
    public double? Total { get; init; }
    public double? Over { get; init; }
    public double? Under { get; init; }
}
