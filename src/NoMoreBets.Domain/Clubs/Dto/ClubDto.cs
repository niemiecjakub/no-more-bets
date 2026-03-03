using NoMoreBets.Domain.Leagues.Dto;

namespace NoMoreBets.Domain.Clubs.Dto;
public record ClubDto(
    int Position,
    string TeamName,
    string TeamShortname,
    int TeamId,
    string TeamLogoUrl,
    int MatchesPlayed,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    string GoalDifference,
    int Points,
    IReadOnlyList<string> Form,
    int? NextOpponentId,
    string? NextOpponentName,
    string? NextOpponentLogoUrl)
{
  public static ClubDto From(TableEntry source) =>
      new(
          source.Position,
          source.TeamName,
          source.TeamShortname,
          source.TeamId,
          source.TeamLogoUrl,
          source.MatchesPlayed,
          source.Wins,
          source.Draws,
          source.Losses,
          source.GoalsFor,
          source.GoalsAgainst,
          source.GoalDifference,
          source.Points,
          source.Form.Select(m => m.ToString()).ToList(),
          source.NextOpponentId,
          source.NextOpponentName,
          source.NextOpponentLogoUrl);
}