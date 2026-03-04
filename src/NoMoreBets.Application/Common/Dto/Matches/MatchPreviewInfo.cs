namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Match preview information.</summary>
public record MatchPreviewInfo
{
    public bool HasPreview { get; init; }
    public int WordCount { get; init; }
}
