using NoMoreBets.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoMoreBets.Domain.Betting;

public class BettingOddsSnapshotRow
{
  public long Id { get; set; }
  public long SnapshotId { get; set; }
  public string EventJson { get; set; } = null!;
  public int EventTypeId { get; set; }

  public BettingOddsSnapshot Snapshot { get; set; } = null!;
  public BettingEventTypeEntity EventTypeEntity { get; set; } = null!;

  [NotMapped]
  public BettingEventType EventType
  {
    get => (BettingEventType)EventTypeId;
    set => EventTypeId = (int)value;
  }
}
