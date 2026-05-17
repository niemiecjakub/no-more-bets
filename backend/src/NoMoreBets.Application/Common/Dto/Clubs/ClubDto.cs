using NoMoreBets.Application.Common.Dto.Leagues;

namespace NoMoreBets.Application.Common.Dto.Clubs;

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
    string? NextOpponentLogoUrl);