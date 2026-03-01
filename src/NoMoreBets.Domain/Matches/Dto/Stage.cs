namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Stage with its matches.</summary>
public record Stage
{
    public int StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public IReadOnlyList<Match> Matches { get; init; } = [];
}
