namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Name from SoccerData API.</summary>
public record Player
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
