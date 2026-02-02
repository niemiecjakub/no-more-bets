namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Basic team information (id and name).</summary>
public record TeamInfo
{
    public int Id { get; init; }
    public required string Name { get; init; }
}
