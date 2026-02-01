namespace NoMoreBets.Features.SoccerData.Model;

/// <summary>Stage with its matches.</summary>
public record Stage
{
    public int StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public IReadOnlyList<Match> Matches { get; init; } = [];
}
