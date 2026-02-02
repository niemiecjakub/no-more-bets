namespace NoMoreBets.Features.MatchAnalysis.Model;

/// <summary>Single item in match preview content.</summary>
public record PreviewContentItem
{
    public required string Name { get; init; }
    public required string Content { get; init; }
}
