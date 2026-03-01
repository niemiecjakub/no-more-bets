namespace NoMoreBets.Domain.Matches.Dto;

/// <summary>Single upcoming match preview summary.</summary>
public record UpcomingMatchPreview
{
    public int Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public double ExcitementRating { get; init; }
    public Teams Teams { get; init; } = null!;
}
