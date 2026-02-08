namespace NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for club overview (recent games and daily summary).</summary>
public record ClubOverviewDto(IReadOnlyList<ClubRecentGameDto> RecentGames, IReadOnlyList<string> DailySummary)
{
    public static ClubOverviewDto From(ClubOverview source) =>
        new(
            source.RecentGames.Select(ClubRecentGameDto.From).ToList(),
            source.DailySummary);
}
