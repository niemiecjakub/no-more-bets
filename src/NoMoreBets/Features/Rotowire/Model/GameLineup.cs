using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Rotowire.Model;

/// <summary>
/// Represents a complete game with lineups.
/// </summary>
public record GameLineup
{
  public required DateTime Date { get; init; }
  public string? Time { get; init; }
  public required TeamLineup HomeTeam { get; init; }
  public required TeamLineup AwayTeam { get; init; }

  public static GameLineup Empty(UpcomingGame game)
  {
    return new GameLineup
    {
      Date = game.Date,
      Time = game.Time,
      HomeTeam = new TeamLineup
      {
        TeamName = game.HomeTeam,
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      },
      AwayTeam = new TeamLineup
      {
        TeamName = game.AwayTeam,
        LineupType = Domain.Enums.LineupType.Unknown,
        Players = Array.Empty<PlayerInLineup>(),
        Injuries = Array.Empty<InjuryEntry>()
      }
    };
  }
}
