using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public record BetSelectionRecord(int MatchId, BettingEventType EventType, string OutcomeKey);

public class BetSelection
{
  public int Id { get; set; }
  public int BetSlipId { get; set; }
  public int MatchId { get; set; }
  public int EventTypeId { get; set; }
  public string OutcomeKey { get; set; } = null!;
  public decimal OddsAtPlacement { get; set; }
  public int StatusId { get; set; }

  public BetSlip BetSlip { get; set; } = null!;
  public Match Match { get; set; } = null!;
  public BettingEventTypeEntity EventTypeEntity { get; set; } = null!;
  public BetStatusEntity BetStatusEntity { get; set; } = null!;

  [NotMapped]
  public BetStatus BetStatus
  {
    get => (BetStatus)StatusId;
    set => StatusId = (int)value;
  }
}
