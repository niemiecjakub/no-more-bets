namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;

/// <summary>API response DTO for an upcoming game. Id and SoccerdataId are from DB Game when matched; otherwise 0 and null.</summary>
public record UpcomingGameDto(
    int Id,
    int? SoccerdataId,
    DateTime Date,
    UpcomingGameTeamDto HomeTeam,
    UpcomingGameTeamDto AwayTeam,
    string Url);
