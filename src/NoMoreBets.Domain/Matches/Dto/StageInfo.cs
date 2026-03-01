namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Stage information from SoccerData API.</summary>
public record StageInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
