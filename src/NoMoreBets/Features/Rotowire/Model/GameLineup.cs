using NoMoreBets.Features.Betclic.GetBetclicUpcomingGames.Dtos;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Rotowire.Model;

/// <summary>
/// Represents a complete game with lineups.
/// </summary>
public record GameLineup
{
  public required DateTime Date { get; init; }
  public string? Time { get; init; }
  public required string HomeTeamName { get; init; }
  public string? HomeTeamCode { get; init; }
  public required TeamLineup HomeTeam { get; init; }
  public required string AwayTeamName { get; init; }
  public string? AwayTeamCode { get; init; }
  public required TeamLineup AwayTeam { get; init; }

  public static GameLineup Empty(UpcomingGame game)
  {
    return new GameLineup
    {
      Date = game.Date,
      Time = game.Time,
      HomeTeamName = game.HomeTeam,
      AwayTeamName = game.AwayTeam,
      HomeTeam = new TeamLineup
      {
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      },
      AwayTeam = new TeamLineup
      {
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      }
    };
  }

  public static GameLineup Empty(UpcomingGameDto dto)
  {
    var timeStr = TimeOnly.FromDateTime(dto.Date).ToString("HH:mm");
    return new GameLineup
    {
      Date = dto.Date,
      Time = timeStr,
      HomeTeamName = dto.HomeTeam.Name,
      AwayTeamName = dto.AwayTeam.Name,
      HomeTeam = new TeamLineup
      {
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      },
      AwayTeam = new TeamLineup
      {
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      }
    };
  }
}
