namespace NoMoreBets.Application.Common.Dto.Matches;

/// <summary>Single item in match preview content.</summary>
public record PreviewContentItem
{
    public string Name { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
