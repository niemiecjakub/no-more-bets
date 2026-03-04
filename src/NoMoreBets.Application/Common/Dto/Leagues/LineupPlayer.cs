namespace NoMoreBets.Application.Common.Dto.Leagues;

/// <summary>Single player in a match lineup from FotMob match detail page.</summary>
public class LineupPlayer
{
    public required string Name { get; init; }
    public double? Rating { get; init; }
}
