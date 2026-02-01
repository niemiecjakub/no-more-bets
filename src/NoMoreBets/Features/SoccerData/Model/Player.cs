namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Player from SoccerData API.</summary>
public record Player
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
