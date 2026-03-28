using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public class BettingOddsSnapshotRow
{
  public long Id { get; set; }
  public long SnapshotId { get; set; }
  public int EventTypeId { get; set; }
  public int? EventOptionId { get; set; }
  public decimal? Odds { get; set; }

  public BettingOddsSnapshot Snapshot { get; set; } = null!;
  public BettingEventTypeEntity EventTypeEntity { get; set; } = null!;
  public BettingEventOptionEntity? EventOptionEntity { get; set; }

  [NotMapped]
  public BettingEventType EventType
  {
    get => (BettingEventType)EventTypeId;
    set => EventTypeId = (int)value;
  }

  [NotMapped]
  public BettingEventOption? EventOption
  {
    get => EventOptionId.HasValue ? (BettingEventOption)EventOptionId.Value : null;
    set => EventOptionId = value.HasValue ? (int)value.Value : null;
  }
}
