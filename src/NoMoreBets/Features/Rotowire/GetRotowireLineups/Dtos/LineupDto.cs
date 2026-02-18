using System.Linq;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Features.Rotowire.Model;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;

/// <summary>API response DTO for a player in a lineup.</summary>
public record PlayerInLineupDto(string Name, PlayerPositionDto Position)
{
  public static PlayerInLineupDto From(PlayerInLineup source) =>
      new(source.Player, PlayerPositionDto.From(source.Position));
}

/// <summary>API response DTO for an injury entry.</summary>
public record InjuryEntryDto(string Name, PlayerPositionDto Position, InjuryStatusDto Status)
{
  public static InjuryEntryDto From(InjuryEntry source) =>
      new(source.Player, PlayerPositionDto.From(source.Position), InjuryStatusDto.From(source.Status));
}

/// <summary>API response DTO for a team lineup.</summary>
public record TeamLineupDto(
    string TeamName,
    string? TeamCode,
    string LineupType,
    IReadOnlyList<PlayerInLineupDto> Players,
    IReadOnlyList<InjuryEntryDto> Injuries)
{
  public static TeamLineupDto From(TeamLineup source, string teamName, string? teamCode) =>
      new(
          teamName,
          teamCode,
          LineupTypes.GetDisplayName(source.LineupType),
          source.Players.Select(PlayerInLineupDto.From).ToList(),
          source.Injuries.Select(InjuryEntryDto.From).ToList());
}

/// <summary>API response DTO for a game lineup.</summary>
public record GameLineupDto(
    DateTime Date,
    string? Time,
    TeamLineupDto HomeTeam,
    TeamLineupDto AwayTeam)
{
  public static GameLineupDto From(GameLineup source) =>
      new(
          source.Date,
          source.Time,
          TeamLineupDto.From(source.HomeTeam, source.HomeTeamName, source.HomeTeamCode),
          TeamLineupDto.From(source.AwayTeam, source.AwayTeamName, source.AwayTeamCode));
}
