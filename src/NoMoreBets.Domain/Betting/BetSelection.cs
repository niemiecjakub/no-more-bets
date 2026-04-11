using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public record BetSelectionRecord(int MatchId, BettingEventType EventType, BettingEventOption EventOption);

public class BetSelection
{
  public int Id { get; set; }
  public int BetSlipId { get; set; }
  public int MatchId { get; set; }
  public int EventTypeId { get; set; }
  public int EventOptionId { get; set; }
  public decimal OddsAtPlacement { get; set; }

  public int StatusId { get; private set; }

  public DateTime? UpdatedAt { get; set; }

  public void SetStatus(BetStatus status)
  {
    var id = (int)status;
    if (StatusId == id)
    {
      return;
    }

    StatusId = id;
    UpdatedAt = DateTime.UtcNow;
    BetSlip?.TouchUpdatedAt();
  }

  public BetSlip BetSlip { get; set; } = null!;
  public Match Match { get; set; } = null!;
  public BettingEventTypeEntity EventTypeEntity { get; set; } = null!;
  public BettingEventOptionEntity EventOptionEntity { get; set; } = null!;
  public BetStatusEntity BetStatusEntity { get; set; } = null!;

  [NotMapped]
  public BetStatus BetStatus
  {
    get => (BetStatus)StatusId;
    set => SetStatus(value);
  }

  [NotMapped]
  public BettingEventType BetEventType
  {
    get => (BettingEventType)EventTypeId;
    set => EventTypeId = (int)value;
  }

  [NotMapped]
  public BettingEventOption BetEventOption
  {
    get => (BettingEventOption)EventOptionId;
    set => EventOptionId = (int)value;
  }
}
