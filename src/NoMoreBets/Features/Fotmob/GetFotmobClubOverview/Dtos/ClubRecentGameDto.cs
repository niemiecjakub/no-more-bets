namespace NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;

using NoMoreBets.Features.Fotmob.Model;

/// <summary>API response DTO for a club's recent game.</summary>
public record ClubRecentGameDto(int OpponentId, string Score, MatchResult Result, string GameUrl)
{
    public static ClubRecentGameDto From(ClubRecentGame source) =>
        new(source.OpponentId, source.Score, source.Result, source.GameUrl);
}
