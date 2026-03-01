namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Represents basic team information from SoccerData API.</summary>
public record TeamInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
