namespace NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;

/// <summary>Team/club info for an upcoming game. Id and SoccerdataId are 0 when not matched from DB.</summary>
public record UpcomingGameTeamDto(int Id, string Name, int SoccerdataId);
