using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Matches;
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
}

public record TeamLineup
{
  public required LineupType LineupType { get; init; }
  public IReadOnlyList<PlayerInLineup> Players { get; init; } = Array.Empty<PlayerInLineup>();
  public IReadOnlyList<InjuryEntry> Injuries { get; init; } = Array.Empty<InjuryEntry>();
}

public record InjuryEntry(FootballPosition Position, string Player, InjuryStatus Status) : PlayerInLineup(Position, Player);

public record PlayerInLineup(FootballPosition Position, string Player);
