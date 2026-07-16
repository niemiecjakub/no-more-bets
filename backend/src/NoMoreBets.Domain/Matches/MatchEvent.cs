using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Players;

namespace NoMoreBets.Domain.Matches;

public class MatchEvent
{
  public int Id { get; set; }
  public int EventTypeId { get; set; }
  public int PlayerId { get; set; }
  public int MatchId { get; set; }
  public int ClubId { get; set; }
  public int Minute { get; set; }

  public MatchEventTypeEntity EventTypeEntity { get; set; } = null!;
  public Player Player { get; set; } = null!;
  public Match Match { get; set; } = null!;
  public Club Club { get; set; } = null!;

  public static MatchEvent Create(
    int matchId,
    int clubId,
    Player player,
    MatchEventType eventType,
    int minute) =>
    new()
    {
      MatchId = matchId,
      ClubId = clubId,
      Player = player,
      EventTypeId = (int)eventType,
      Minute = minute
    };

  public string FormatEmbeddingText()
  {
    var playerName = Player?.Name ?? "Unknown";
    return $"{Minute}' {(MatchEventType)EventTypeId} {playerName}";
  }
}
