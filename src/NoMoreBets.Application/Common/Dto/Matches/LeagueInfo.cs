namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>League information from SoccerData API.</summary>
public record LeagueInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
