namespace NoMoreBets.Domain.Leagues.Dto;

/// <summary>Group of statistics (e.g. Possession, Shots) from FotMob Statistics tab.</summary>
public class StatGroup
{
    public required string Title { get; init; }
    public required IReadOnlyList<StatRow> Rows { get; init; }
}
