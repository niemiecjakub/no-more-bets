namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>League information from SoccerData API.</summary>
public record LeagueInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
