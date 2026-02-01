using System.Linq;
using NoMoreBets.Domain.Entities.Rotowire;

namespace NoMoreBets.Common;

/// <summary>API response DTO for a player in a lineup.</summary>
public record PlayerInLineupDto(string Name, PositionDto Position)
{
  public static PlayerInLineupDto From(PlayerInLineup source) =>
      new(source.Player, PositionDto.From(source.Position));
}

/// <summary>API response DTO for an injury entry.</summary>
public record InjuryEntryDto(string Name, PositionDto Position, StatusDto Status)
{
  public static InjuryEntryDto From(InjuryEntry source) =>
      new(source.Player, PositionDto.From(source.Position), StatusDto.From(source.Status));
}

/// <summary>API response DTO for a team lineup.</summary>
public record TeamLineupDto(
    string TeamName,
    string? TeamCode,
    string LineupType,
    IReadOnlyList<PlayerInLineupDto> Players,
    IReadOnlyList<InjuryEntryDto> Injuries)
{
  public static TeamLineupDto From(TeamLineup source) =>
      new(
          source.TeamName,
          source.TeamCode,
          source.LineupType,
          source.Players.Select(PlayerInLineupDto.From).ToList(),
          source.Injuries.Select(InjuryEntryDto.From).ToList());
}

/// <summary>API response DTO for a game lineup.</summary>
public record GameLineupDto(
    string Date,
    string? Time,
    TeamLineupDto HomeTeam,
    TeamLineupDto AwayTeam)
{
  public static GameLineupDto From(GameLineup source) =>
      new(
          source.Date,
          source.Time,
          TeamLineupDto.From(source.HomeTeam),
          TeamLineupDto.From(source.AwayTeam));
}
